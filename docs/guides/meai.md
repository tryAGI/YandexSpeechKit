# Microsoft.Extensions.AI Integration

!!! tip "Cross-SDK comparison"
    See the [centralized MEAI documentation](https://tryagi.github.io/docs/meai/) for feature matrices and comparisons across all tryAGI SDKs.

The YandexSpeechKit SDK provides integration with [Microsoft.Extensions.AI](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai), enabling seamless interoperability with the unified .NET AI abstractions.

## Installation

```bash
dotnet add package YandexSpeechKit
```

## Usage

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

## Next Steps

- Check the [Examples](../index.md) for complete working code
- See the [centralized MEAI docs](https://tryagi.github.io/docs/meai/) for cross-SDK comparisons
- Visit the [Microsoft.Extensions.AI documentation](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai) for framework details
