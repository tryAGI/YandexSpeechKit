# Generate

Transcribe a WAV file with Yandex SpeechKit v3 and Microsoft.Extensions.AI.

This example assumes `using YandexSpeechKit;` is in scope and `apiKey` contains your YandexSpeechKit API key.

```csharp
using var client = new YandexSpeechKitClient(apiKey);
var samplePath =
    Environment.GetEnvironmentVariable("YANDEX_SPEECHKIT_SAMPLE_WAV") is { Length: > 0 } samplePathValue
        ? samplePathValue
        : throw new AssertInconclusiveException("YANDEX_SPEECHKIT_SAMPLE_WAV environment variable is not found.");

await using var audio = File.OpenRead(samplePath);
var response = await client.GetTextAsync(audio, new SpeechToTextOptions
{
    SpeechLanguage = "ru-RU",
});
```