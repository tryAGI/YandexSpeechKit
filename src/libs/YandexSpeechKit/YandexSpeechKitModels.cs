namespace YandexSpeechKit;

/// <summary>A word with provider timestamps.</summary>
public sealed record YandexSpeechWord(string Text, TimeSpan StartTime, TimeSpan EndTime);

/// <summary>A language hypothesis returned by SpeechKit.</summary>
public sealed record YandexSpeechLanguage(string LanguageCode, double Probability);

/// <summary>A recognition alternative returned by SpeechKit.</summary>
public sealed record YandexSpeechAlternative(
    string Text,
    TimeSpan StartTime,
    TimeSpan EndTime,
    double Confidence,
    IReadOnlyList<YandexSpeechWord> Words,
    IReadOnlyList<YandexSpeechLanguage> Languages,
    string? ChannelTag);
