namespace YandexSpeechKit.IntegrationTests;

[TestClass]
public partial class Tests
{
    private static YandexSpeechKitClient GetAuthenticatedClient()
    {
        var apiKey =
            Environment.GetEnvironmentVariable("YANDEX_SPEECHKIT_API_KEY") is { Length: > 0 } apiKeyValue
                ? apiKeyValue
                : Environment.GetEnvironmentVariable("YANDEXSPEECHKIT_API_KEY") is { Length: > 0 } legacyApiKeyValue
                    ? legacyApiKeyValue
                    : throw new AssertInconclusiveException("YANDEX_SPEECHKIT_API_KEY environment variable is not found.");

        var folderId = Environment.GetEnvironmentVariable("YANDEX_SPEECHKIT_FOLDER_ID") is { Length: > 0 } folderIdValue
            ? folderIdValue
            : null;
        var client = new YandexSpeechKitClient(apiKey, folderId);

        return client;
    }
}
