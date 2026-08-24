#nullable enable
#pragma warning disable MEAI001

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.AI;
using Speechkit.Stt.V3;

namespace YandexSpeechKit;

/// <summary>
/// Yandex SpeechKit v3 gRPC client with raw generated clients and a Microsoft.Extensions.AI adapter.
/// </summary>
public sealed class YandexSpeechKitClient : ISpeechToTextClient, IDisposable
{
    public const string DefaultEndpoint = "https://stt.api.cloud.yandex.net:443";
    public const string DefaultModel = "general";

    private readonly GrpcChannel? _ownedChannel;
    private readonly Uri _endpoint;
    private readonly string _defaultModelId;
    private SpeechToTextClientMetadata? _metadata;

    /// <summary>Creates a client using a Yandex Cloud API key.</summary>
    public YandexSpeechKitClient(
        string apiKey,
        string? folderId = null,
        Uri? endpoint = null,
        string defaultModelId = DefaultModel)
        : this(YandexSpeechKitCredentials.FromApiKey(apiKey), folderId, endpoint, defaultModelId)
    {
    }

    /// <summary>Creates a client using refreshable credentials.</summary>
    public YandexSpeechKitClient(
        YandexSpeechKitCredentials credentials,
        string? folderId = null,
        Uri? endpoint = null,
        string defaultModelId = DefaultModel)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        _endpoint = endpoint ?? new Uri(DefaultEndpoint);
        if (_endpoint.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("Yandex SpeechKit credentials require an HTTPS endpoint.", nameof(endpoint));
        }

        _defaultModelId = !string.IsNullOrWhiteSpace(defaultModelId)
            ? defaultModelId
            : throw new ArgumentException("A default model ID is required.", nameof(defaultModelId));

        var callCredentials = CallCredentials.FromInterceptor(async (context, metadata) =>
        {
            var authorization = await credentials
                .GetAuthorizationHeaderAsync(context.CancellationToken)
                .ConfigureAwait(false);
            metadata.Add("authorization", authorization);
            if (!string.IsNullOrWhiteSpace(folderId))
            {
                metadata.Add("x-folder-id", folderId);
            }
        });

        _ownedChannel = GrpcChannel.ForAddress(
            _endpoint,
            new GrpcChannelOptions
            {
                Credentials = ChannelCredentials.Create(new SslCredentials(), callCredentials),
            });

        var callInvoker = _ownedChannel.CreateCallInvoker();
        Recognizer = new Recognizer.RecognizerClient(callInvoker);
        AsyncRecognizer = new AsyncRecognizer.AsyncRecognizerClient(callInvoker);
    }

    /// <summary>Creates a client over a caller-owned invoker, primarily for custom transports and tests.</summary>
    public YandexSpeechKitClient(
        CallInvoker callInvoker,
        Uri? endpoint = null,
        string defaultModelId = DefaultModel)
    {
        ArgumentNullException.ThrowIfNull(callInvoker);
        _endpoint = endpoint ?? new Uri(DefaultEndpoint);
        _defaultModelId = !string.IsNullOrWhiteSpace(defaultModelId)
            ? defaultModelId
            : throw new ArgumentException("A default model ID is required.", nameof(defaultModelId));
        Recognizer = new Recognizer.RecognizerClient(callInvoker);
        AsyncRecognizer = new AsyncRecognizer.AsyncRecognizerClient(callInvoker);
    }

    /// <summary>The raw generated streaming recognition client.</summary>
    public Recognizer.RecognizerClient Recognizer { get; }

    /// <summary>The raw generated asynchronous file recognition client.</summary>
    public AsyncRecognizer.AsyncRecognizerClient AsyncRecognizer { get; }

    public void Dispose() => _ownedChannel?.Dispose();

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        return serviceKey is not null ? null :
            serviceType == typeof(SpeechToTextClientMetadata)
                ? (_metadata ??= new("yandex-speechkit", _endpoint, _defaultModelId)) :
            serviceType.IsInstanceOfType(this) ? this :
            serviceType.IsInstanceOfType(Recognizer) ? Recognizer :
            serviceType.IsInstanceOfType(AsyncRecognizer) ? AsyncRecognizer :
            null;
    }

    public async Task<SpeechToTextResponse> GetTextAsync(
        Stream audioSpeechStream,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(audioSpeechStream);

        var finals = new SortedDictionary<long, YandexSpeechAlternative>();
        var rawResponses = new List<StreamingResponse>();
        string? serverSessionId = null;
        string? fallbackText = null;
        long nextFinalIndex = 0;

        await foreach (var response in RecognizeAsync(
            audioSpeechStream,
            options,
            RecognitionModelOptions.Types.AudioProcessingType.FullData,
            cancellationToken).ConfigureAwait(false))
        {
            rawResponses.Add(response);
            serverSessionId ??= NullIfEmpty(response.SessionUuid?.Uuid);

            if (response.EventCase == StreamingResponse.EventOneofCase.Final)
            {
                var responseAlternatives = CreateAlternatives(response.Final, response.ChannelTag);
                var alternative = responseAlternatives.Length > 0 ? responseAlternatives[0] : null;
                if (alternative is not null)
                {
                    var finalIndex = response.AudioCursors?.FinalIndex ?? nextFinalIndex;
                    while (finals.ContainsKey(finalIndex))
                    {
                        finalIndex = ++nextFinalIndex;
                    }

                    finals[finalIndex] = alternative;
                    nextFinalIndex = Math.Max(nextFinalIndex, finalIndex + 1);
                }
            }
            else if (response.EventCase == StreamingResponse.EventOneofCase.FinalRefinement
                && response.FinalRefinement.TypeCase == FinalRefinement.TypeOneofCase.NormalizedText)
            {
                var responseAlternatives = CreateAlternatives(
                    response.FinalRefinement.NormalizedText,
                    response.ChannelTag);
                var alternative = responseAlternatives.Length > 0 ? responseAlternatives[0] : null;
                if (alternative is not null)
                {
                    finals[response.FinalRefinement.FinalIndex] = alternative;
                }
            }
            else if (response.EventCase == StreamingResponse.EventOneofCase.Partial)
            {
                fallbackText = response.Partial.Alternatives.FirstOrDefault()?.Text;
            }
        }

        var alternatives = finals.Values.ToArray();
        var words = alternatives.SelectMany(static alternative => alternative.Words).ToArray();
        var text = alternatives.Length > 0
            ? string.Join(' ', alternatives.Select(static alternative => alternative.Text).Where(static text => text.Length > 0))
            : fallbackText ?? string.Empty;

        var properties = new AdditionalPropertiesDictionary
        {
            [YandexSpeechKitPropertyNames.Alternatives] = alternatives,
            [YandexSpeechKitPropertyNames.Words] = words,
        };
        if (serverSessionId is not null)
        {
            properties[YandexSpeechKitPropertyNames.ServerSessionId] = serverSessionId;
        }

        return new SpeechToTextResponse(text)
        {
            ResponseId = serverSessionId,
            ModelId = GetModelId(options),
            StartTime = words.Length > 0 ? words.Min(static word => word.StartTime) : null,
            EndTime = words.Length > 0 ? words.Max(static word => word.EndTime) : null,
            RawRepresentation = rawResponses,
            AdditionalProperties = properties,
        };
    }

    public async IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingTextAsync(
        Stream audioSpeechStream,
        SpeechToTextOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(audioSpeechStream);

        var responseId = Guid.NewGuid().ToString("N");
        var modelId = GetModelId(options);
        yield return new SpeechToTextResponseUpdate
        {
            Kind = SpeechToTextResponseUpdateKind.SessionOpen,
            ResponseId = responseId,
            ModelId = modelId,
        };

        await foreach (var response in RecognizeAsync(
            audioSpeechStream,
            options,
            RecognitionModelOptions.Types.AudioProcessingType.RealTime,
            cancellationToken).ConfigureAwait(false))
        {
            if (CreateUpdate(response, responseId, modelId) is { } update)
            {
                yield return update;
            }
        }

        yield return new SpeechToTextResponseUpdate
        {
            Kind = SpeechToTextResponseUpdateKind.SessionClose,
            ResponseId = responseId,
            ModelId = modelId,
        };
    }

    /// <summary>Maps a raw streaming response to a Microsoft.Extensions.AI update.</summary>
    public static SpeechToTextResponseUpdate? CreateUpdate(
        StreamingResponse response,
        string responseId,
        string? modelId = null)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentException.ThrowIfNullOrWhiteSpace(responseId);

        AlternativeUpdate? source;
        SpeechToTextResponseUpdateKind kind;
        var isRefinement = false;
        long? finalIndex = null;

        switch (response.EventCase)
        {
            case StreamingResponse.EventOneofCase.Partial:
                source = response.Partial;
                kind = SpeechToTextResponseUpdateKind.TextUpdating;
                break;
            case StreamingResponse.EventOneofCase.Final:
                source = response.Final;
                kind = SpeechToTextResponseUpdateKind.TextUpdated;
                finalIndex = response.AudioCursors?.FinalIndex;
                break;
            case StreamingResponse.EventOneofCase.FinalRefinement
                when response.FinalRefinement.TypeCase == FinalRefinement.TypeOneofCase.NormalizedText:
                source = response.FinalRefinement.NormalizedText;
                kind = SpeechToTextResponseUpdateKind.TextUpdating;
                isRefinement = true;
                finalIndex = response.FinalRefinement.FinalIndex;
                break;
            default:
                return null;
        }

        var alternatives = CreateAlternatives(source, response.ChannelTag);
        var primary = alternatives.Length > 0 ? alternatives[0] : null;
        if (primary is null)
        {
            return null;
        }

        var properties = new AdditionalPropertiesDictionary
        {
            [YandexSpeechKitPropertyNames.Alternatives] = alternatives,
            [YandexSpeechKitPropertyNames.Words] = primary.Words,
        };
        if (!string.IsNullOrWhiteSpace(response.ChannelTag))
        {
            properties[YandexSpeechKitPropertyNames.ChannelTag] = response.ChannelTag;
        }
        if (response.AudioCursors is not null)
        {
            properties[YandexSpeechKitPropertyNames.AudioCursors] = response.AudioCursors;
        }
        if (finalIndex.HasValue)
        {
            properties[YandexSpeechKitPropertyNames.FinalIndex] = finalIndex.Value;
        }
        if (isRefinement)
        {
            properties[YandexSpeechKitPropertyNames.IsRefinement] = true;
        }
        if (NullIfEmpty(response.SessionUuid?.Uuid) is { } serverSessionId)
        {
            properties[YandexSpeechKitPropertyNames.ServerSessionId] = serverSessionId;
        }

        return new SpeechToTextResponseUpdate(primary.Text)
        {
            Kind = kind,
            ResponseId = responseId,
            ModelId = modelId,
            StartTime = primary.StartTime,
            EndTime = primary.EndTime,
            RawRepresentation = response,
            AdditionalProperties = properties,
        };
    }

    private async IAsyncEnumerable<StreamingResponse> RecognizeAsync(
        Stream audioSpeechStream,
        SpeechToTextOptions? options,
        RecognitionModelOptions.Types.AudioProcessingType processingType,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var call = Recognizer.RecognizeStreaming(cancellationToken: linkedCancellation.Token);
        var writerTask = WriteRequestsAsync(
            call.RequestStream,
            audioSpeechStream,
            options,
            processingType,
            linkedCancellation.Token);

        try
        {
            while (await call.ResponseStream.MoveNext(linkedCancellation.Token).ConfigureAwait(false))
            {
                yield return call.ResponseStream.Current;
            }

            await writerTask.ConfigureAwait(false);
        }
        finally
        {
            await linkedCancellation.CancelAsync().ConfigureAwait(false);
            if (!writerTask.IsCompleted)
            {
                try
                {
                    await writerTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (linkedCancellation.IsCancellationRequested)
                {
                }
            }
        }
    }

    private async Task WriteRequestsAsync(
        IClientStreamWriter<StreamingRequest> requestStream,
        Stream audioSpeechStream,
        SpeechToTextOptions? options,
        RecognitionModelOptions.Types.AudioProcessingType processingType,
        CancellationToken cancellationToken)
    {
        await requestStream.WriteAsync(
            new StreamingRequest
            {
                SessionOptions = CreateStreamingOptions(options, processingType),
            },
            cancellationToken).ConfigureAwait(false);

        var chunkSize = GetInt32(options, YandexSpeechKitPropertyNames.ChunkSize, 32 * 1024);
        if (chunkSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "The Yandex SpeechKit chunk size must be positive.");
        }

        var buffer = ArrayPool<byte>.Shared.Rent(chunkSize);
        try
        {
            int read;
            while ((read = await audioSpeechStream
                .ReadAsync(buffer.AsMemory(0, chunkSize), cancellationToken)
                .ConfigureAwait(false)) > 0)
            {
                await requestStream.WriteAsync(
                    new StreamingRequest
                    {
                        Chunk = new AudioChunk
                        {
                            Data = ByteString.CopyFrom(buffer, 0, read),
                        },
                    },
                    cancellationToken).ConfigureAwait(false);
            }

            await requestStream.CompleteAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private StreamingOptions CreateStreamingOptions(
        SpeechToTextOptions? options,
        RecognitionModelOptions.Types.AudioProcessingType processingType)
    {
        var recognitionModel = new RecognitionModelOptions
        {
            Model = GetModelId(options),
            AudioFormat = CreateAudioFormat(options),
            AudioProcessingType = processingType,
            TextNormalization = new TextNormalizationOptions
            {
                TextNormalization = GetBoolean(
                    options,
                    YandexSpeechKitPropertyNames.EnableTextNormalization,
                    defaultValue: false)
                    ? TextNormalizationOptions.Types.TextNormalization.Enabled
                    : TextNormalizationOptions.Types.TextNormalization.Disabled,
            },
        };

        if (!string.IsNullOrWhiteSpace(options?.SpeechLanguage))
        {
            recognitionModel.LanguageRestriction = new LanguageRestrictionOptions
            {
                RestrictionType = LanguageRestrictionOptions.Types.LanguageRestrictionType.Whitelist,
                LanguageCode = { options.SpeechLanguage },
            };
        }

        return new StreamingOptions
        {
            RecognitionModel = recognitionModel,
            SpeakerLabeling = new SpeakerLabelingOptions
            {
                SpeakerLabeling = GetBoolean(
                    options,
                    YandexSpeechKitPropertyNames.EnableSpeakerLabeling,
                    defaultValue: false)
                    ? SpeakerLabelingOptions.Types.SpeakerLabeling.Enabled
                    : SpeakerLabelingOptions.Types.SpeakerLabeling.Disabled,
            },
        };
    }

    private static AudioFormatOptions CreateAudioFormat(SpeechToTextOptions? options)
    {
        var audioFormat = GetString(options, YandexSpeechKitPropertyNames.AudioFormat) ?? "wav";
        return audioFormat.ToUpperInvariant() switch
        {
            "PCM_S16LE" or "LINEAR16" or "RAW" => new AudioFormatOptions
            {
                RawAudio = new RawAudio
                {
                    AudioEncoding = RawAudio.Types.AudioEncoding.Linear16Pcm,
                    SampleRateHertz = options?.SpeechSampleRate ?? 16_000,
                    AudioChannelCount = GetInt32(options, YandexSpeechKitPropertyNames.AudioChannelCount, 1),
                },
            },
            "WAV" => CreateContainerAudio(ContainerAudio.Types.ContainerAudioType.Wav),
            "OGG" or "OGG_OPUS" => CreateContainerAudio(ContainerAudio.Types.ContainerAudioType.OggOpus),
            "MP3" => CreateContainerAudio(ContainerAudio.Types.ContainerAudioType.Mp3),
            _ => throw new ArgumentException(
                $"Unsupported Yandex SpeechKit audio format '{audioFormat}'. Use wav, ogg_opus, mp3, or pcm_s16le.",
                nameof(options)),
        };
    }

    private static AudioFormatOptions CreateContainerAudio(ContainerAudio.Types.ContainerAudioType type) =>
        new()
        {
            ContainerAudio = new ContainerAudio
            {
                ContainerAudioType = type,
            },
        };

    private string GetModelId(SpeechToTextOptions? options) =>
        !string.IsNullOrWhiteSpace(options?.ModelId) ? options.ModelId : _defaultModelId;

    private static YandexSpeechAlternative[] CreateAlternatives(
        AlternativeUpdate source,
        string? channelTag)
    {
        return source.Alternatives.Select(alternative => new YandexSpeechAlternative(
            Text: alternative.Text,
            StartTime: TimeSpan.FromMilliseconds(alternative.StartTimeMs),
            EndTime: TimeSpan.FromMilliseconds(alternative.EndTimeMs),
            Confidence: alternative.Confidence,
            Words: alternative.Words.Select(static word => new YandexSpeechWord(
                word.Text,
                TimeSpan.FromMilliseconds(word.StartTimeMs),
                TimeSpan.FromMilliseconds(word.EndTimeMs))).ToArray(),
            Languages: alternative.Languages.Select(static language => new YandexSpeechLanguage(
                language.LanguageCode,
                language.Probability)).ToArray(),
            ChannelTag: NullIfEmpty(channelTag))).ToArray();
    }

    private static string? GetString(SpeechToTextOptions? options, string key) =>
        options?.AdditionalProperties?.TryGetValue(key, out var value) == true ? value as string : null;

    private static int GetInt32(SpeechToTextOptions? options, string key, int defaultValue)
    {
        if (options?.AdditionalProperties?.TryGetValue(key, out var value) != true)
        {
            return defaultValue;
        }

        return value switch
        {
            int intValue => intValue,
            long longValue when longValue is >= int.MinValue and <= int.MaxValue => (int)longValue,
            _ => defaultValue,
        };
    }

    private static bool GetBoolean(SpeechToTextOptions? options, string key, bool defaultValue) =>
        options?.AdditionalProperties?.TryGetValue(key, out var value) == true && value is bool boolValue
            ? boolValue
            : defaultValue;

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
