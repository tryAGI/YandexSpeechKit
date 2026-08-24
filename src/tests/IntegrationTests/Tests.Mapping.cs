using Microsoft.Extensions.AI;
using Speechkit.Stt.V3;

namespace YandexSpeechKit.IntegrationTests;

public partial class Tests
{
    [TestMethod]
    public void FinalResponse_MapsTextTimingsAndProviderMetadata()
    {
        var response = new StreamingResponse
        {
            SessionUuid = new SessionUuid
            {
                Uuid = "server-session",
            },
            AudioCursors = new AudioCursors
            {
                FinalIndex = 7,
            },
            ChannelTag = "speaker-1",
            Final = new AlternativeUpdate
            {
                Alternatives =
                {
                    new Alternative
                    {
                        Text = "привет мир",
                        StartTimeMs = 100,
                        EndTimeMs = 900,
                        Confidence = 0.95,
                        Words =
                        {
                            new Word
                            {
                                Text = "привет",
                                StartTimeMs = 100,
                                EndTimeMs = 450,
                            },
                            new Word
                            {
                                Text = "мир",
                                StartTimeMs = 500,
                                EndTimeMs = 900,
                            },
                        },
                    },
                },
            },
        };

        var update = YandexSpeechKitClient.CreateUpdate(response, "response-id", "general");

        update.Should().NotBeNull();
        update!.Kind.Should().Be(SpeechToTextResponseUpdateKind.TextUpdated);
        update.Text.Should().Be("привет мир");
        update.StartTime.Should().Be(TimeSpan.FromMilliseconds(100));
        update.EndTime.Should().Be(TimeSpan.FromMilliseconds(900));
        update.AdditionalProperties![YandexSpeechKitPropertyNames.ServerSessionId].Should().Be("server-session");
        update.AdditionalProperties[YandexSpeechKitPropertyNames.ChannelTag].Should().Be("speaker-1");
        update.AdditionalProperties[YandexSpeechKitPropertyNames.FinalIndex].Should().Be(7L);

        var words = update.AdditionalProperties[YandexSpeechKitPropertyNames.Words]
            .Should().BeAssignableTo<IReadOnlyList<YandexSpeechWord>>().Subject;
        words.Should().HaveCount(2);
        words[1].Text.Should().Be("мир");
    }

    [TestMethod]
    public void GetService_ExposesMetadataAndRawClients()
    {
        using var client = new YandexSpeechKitClient("test-api-key");

        client.GetService(typeof(SpeechToTextClientMetadata)).Should().BeOfType<SpeechToTextClientMetadata>();
        client.GetService(typeof(Recognizer.RecognizerClient)).Should().BeSameAs(client.Recognizer);
        client.GetService(typeof(AsyncRecognizer.AsyncRecognizerClient)).Should().BeSameAs(client.AsyncRecognizer);
        client.GetService(typeof(string)).Should().BeNull();
    }
}
