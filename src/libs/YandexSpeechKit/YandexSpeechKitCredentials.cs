namespace YandexSpeechKit;

/// <summary>Supplies a fresh Yandex SpeechKit authorization header for every gRPC call.</summary>
public sealed class YandexSpeechKitCredentials
{
    private readonly Func<CancellationToken, ValueTask<string>> _authorizationHeaderProvider;

    private YandexSpeechKitCredentials(Func<CancellationToken, ValueTask<string>> authorizationHeaderProvider)
    {
        _authorizationHeaderProvider = authorizationHeaderProvider;
    }

    /// <summary>Creates credentials backed by a long-lived Yandex Cloud API key.</summary>
    public static YandexSpeechKitCredentials FromApiKey(string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        return FromAuthorizationHeaderProvider(_ => ValueTask.FromResult($"Api-Key {apiKey}"));
    }

    /// <summary>Creates credentials backed by a refreshable IAM-token provider.</summary>
    public static YandexSpeechKitCredentials FromIamTokenProvider(
        Func<CancellationToken, ValueTask<string>> iamTokenProvider)
    {
        ArgumentNullException.ThrowIfNull(iamTokenProvider);

        return FromAuthorizationHeaderProvider(async cancellationToken =>
        {
            var token = await iamTokenProvider(cancellationToken).ConfigureAwait(false);
            return $"Bearer {ValidateCredential(token, "IAM token")}";
        });
    }

    /// <summary>Creates credentials from a provider returning the complete Authorization value.</summary>
    public static YandexSpeechKitCredentials FromAuthorizationHeaderProvider(
        Func<CancellationToken, ValueTask<string>> authorizationHeaderProvider)
    {
        ArgumentNullException.ThrowIfNull(authorizationHeaderProvider);
        return new YandexSpeechKitCredentials(authorizationHeaderProvider);
    }

    internal async ValueTask<string> GetAuthorizationHeaderAsync(CancellationToken cancellationToken)
    {
        var header = await _authorizationHeaderProvider(cancellationToken).ConfigureAwait(false);
        return ValidateCredential(header, "authorization header");
    }

    private static string ValidateCredential(string? value, string name)
    {
        return !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException($"The Yandex SpeechKit {name} provider returned an empty value.");
    }
}
