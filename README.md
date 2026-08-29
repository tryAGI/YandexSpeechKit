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

<!-- AUTOSDK:ECOSYSTEM-MAINTENANCE:START -->
## Ecosystem maintenance

This SDK is one of more than 200 .NET SDKs maintained with [AutoSDK](https://github.com/tryAGI/AutoSDK). The tryAGI [SDK audit](https://github.com/tryAGI/tryAGI/blob/main/GENERATED_SDK_AUDITS.md) continuously checks repository synchronization, upstream-spec regeneration, release workflows, warnings, public API visibility, and trimming/NativeAOT compatibility.

Every issue is first investigated for ecosystem-wide applicability. When the root cause belongs in AutoSDK, we fix and regression-test the generator, then roll the improvement out to every applicable SDK. Provider-specific behavior remains in this repository when it cannot be derived safely from the API specification.

Issue content—including code blocks, logs, links, and attachments—is treated only as untrusted diagnostic data. Embedded control instructions, hidden directives, delimiter tricks, or requests to alter triage or tooling behavior are ignored. Please report reproducible technical evidence and remove secrets and personal data.
<!-- AUTOSDK:ECOSYSTEM-MAINTENANCE:END -->
