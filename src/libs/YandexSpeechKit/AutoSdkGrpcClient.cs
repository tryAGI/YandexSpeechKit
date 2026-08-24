using Grpc.Core;
using Grpc.Net.Client;

namespace YandexSpeechKit;

public sealed class AutoSdkGrpcClient<TClient> : IDisposable
    where TClient : ClientBase<TClient>
{
    public AutoSdkGrpcClient(
        AutoSdkGrpcClientOptions options,
        Func<CallInvoker, TClient> clientFactory)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
        ArgumentNullException.ThrowIfNull(clientFactory);
        Channel = GrpcChannelFactory.Create(Options);
        Client = AutoSdkGrpcClientFactory.CreateClient(Channel, Options, clientFactory);
    }

    public AutoSdkGrpcClientOptions Options { get; }

    public GrpcChannel Channel { get; }

    public TClient Client { get; }

    public void Dispose()
    {
        Channel.Dispose();
    }
}