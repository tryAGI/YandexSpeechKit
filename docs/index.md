# YandexSpeechKit

[![NuGet](https://img.shields.io/nuget/vpre/YandexSpeechKit)](https://www.nuget.org/packages/YandexSpeechKit/)
[![Build](https://github.com/tryAGI/YandexSpeechKit/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/tryAGI/YandexSpeechKit/actions/workflows/dotnet.yml)

Generated .NET gRPC clients for Yandex SpeechKit v3 plus a `Microsoft.Extensions.AI.ISpeechToTextClient` adapter.

The protobuf contract is regenerated from the official MIT-licensed
[`yandex-cloud/cloudapi`](https://github.com/yandex-cloud/cloudapi) repository. Raw generated clients remain available as
`Speechkit.Stt.V3.Recognizer.RecognizerClient` and `Speechkit.Stt.V3.AsyncRecognizer.AsyncRecognizerClient`.

## Installation

```bash
dotnet add package YandexSpeechKit
```

## API key

```csharp
using Microsoft.Extensions.AI;
using YandexSpeechKit;

using var client = new YandexSpeechKitClient(
    apiKey: Environment.GetEnvironmentVariable("YANDEX_SPEECHKIT_API_KEY")!,
    folderId: Environment.GetEnvironmentVariable("YANDEX_SPEECHKIT_FOLDER_ID"));

await using var audio = File.OpenRead("speech.wav");
var response = await client.GetTextAsync(audio, new SpeechToTextOptions
{
    SpeechLanguage = "ru-RU",
});

Console.WriteLine(response.Text);
```

## Refreshable IAM token

```csharp
var credentials = YandexSpeechKitCredentials.FromIamTokenProvider(
    cancellationToken => GetFreshIamTokenAsync(cancellationToken));

using var client = new YandexSpeechKitClient(credentials, folderId);
```

The token callback runs for every gRPC call, so short-lived IAM tokens are not captured permanently by the client.

## Streaming and audio formats

`GetStreamingTextAsync` performs true bidirectional gRPC streaming. WAV is the default input. Set
`YandexSpeechKitPropertyNames.AudioFormat` to `ogg_opus`, `mp3`, or `pcm_s16le` through
`SpeechToTextOptions.AdditionalProperties`. Raw PCM also uses `SpeechSampleRate` and
`YandexSpeechKitPropertyNames.AudioChannelCount`.

Word timings, alternatives, final indices, channel tags, server session IDs, and audio cursors are preserved in
`AdditionalProperties`.

## Regeneration

```bash
cd src/libs/YandexSpeechKit
./generate.sh
```

The script fetches the current official proto closure, records its commit, refreshes the generated gRPC support files,
and preserves the upstream license in the package.
