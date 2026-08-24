#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
work_dir="$(mktemp -d /tmp/yandex-speechkit-generate.XXXXXX)"
trap 'rm -rf "$work_dir"' EXIT

dotnet tool update --global autosdk.cli --prerelease 2>/dev/null ||
  dotnet tool install --global autosdk.cli --prerelease

git clone --depth 1 --recurse-submodules --shallow-submodules \
  https://github.com/yandex-cloud/cloudapi.git "$work_dir/cloudapi"
rsync -a "$work_dir/cloudapi/third_party/googleapis/google/" "$work_dir/cloudapi/google/"
cp "$work_dir/cloudapi/yandex/cloud/ai/stt/v3/stt_service.proto" "$work_dir/cloudapi/yandex-stt-entry.proto"

autosdk generate "$work_dir/cloudapi/yandex-stt-entry.proto" \
  --namespace YandexSpeechKit \
  --targetFramework net10.0 \
  --output "$work_dir/generated"

rsync -a --delete "$work_dir/generated/Protos/" "$script_dir/Protos/"
cp "$work_dir/cloudapi/LICENSE" "$script_dir/Protos/LICENSE"
git -C "$work_dir/cloudapi" rev-parse HEAD > "$script_dir/Protos/UPSTREAM_COMMIT"

for support_file in \
  AutoSdkGrpcCallOptionsInterceptor.cs \
  AutoSdkGrpcClient.cs \
  AutoSdkGrpcClientFactory.cs \
  AutoSdkGrpcClientOptions.cs \
  AutoSdkGrpcServiceCollectionExtensions.cs \
  GrpcChannelFactory.cs; do
  cp "$work_dir/generated/$support_file" "$script_dir/$support_file"
done
