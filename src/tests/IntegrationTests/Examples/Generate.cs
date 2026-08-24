/*
order: 10
title: Generate
slug: generate

Transcribe a WAV file with Yandex SpeechKit v3 and Microsoft.Extensions.AI.
*/

using Microsoft.Extensions.AI;

namespace YandexSpeechKit.IntegrationTests;

public partial class Tests
{
    [TestMethod]
    public async Task Example_TranscribeWav()
    {
        using var client = GetAuthenticatedClient();
        var samplePath =
            Environment.GetEnvironmentVariable("YANDEX_SPEECHKIT_SAMPLE_WAV") is { Length: > 0 } samplePathValue
                ? samplePathValue
                : throw new AssertInconclusiveException("YANDEX_SPEECHKIT_SAMPLE_WAV environment variable is not found.");

        await using var audio = File.OpenRead(samplePath);
        var response = await client.GetTextAsync(audio, new SpeechToTextOptions
        {
            SpeechLanguage = "ru-RU",
        });

        response.Text.Should().NotBeNullOrWhiteSpace();
    }
}
