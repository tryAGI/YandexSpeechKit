using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;

namespace YandexSpeechKit;

public static class AutoSdkGrpcClientFactory
{
    public static AutoSdkGrpcClient<TClient> Create<TClient>(
        Action<AutoSdkGrpcClientOptions> configure,
        Func<CallInvoker, TClient> clientFactory)
        where TClient : ClientBase<TClient>
    {
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(clientFactory);

        var options = new AutoSdkGrpcClientOptions();
        configure(options);
        return new AutoSdkGrpcClient<TClient>(options, clientFactory);
    }

    public static AutoSdkGrpcClient<TClient> Create<TClient>(
        AutoSdkGrpcClientOptions options,
        Func<CallInvoker, TClient> clientFactory)
        where TClient : ClientBase<TClient>
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(clientFactory);

        return new AutoSdkGrpcClient<TClient>(options.Clone(), clientFactory);
    }

    internal static TClient CreateClient<TClient>(
        GrpcChannel channel,
        AutoSdkGrpcClientOptions options,
        Func<CallInvoker, TClient> clientFactory)
        where TClient : ClientBase<TClient>
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(clientFactory);

        var callInvoker = channel
            .CreateCallInvoker()
            .Intercept(new AutoSdkGrpcCallOptionsInterceptor(options));

        return clientFactory(callInvoker) ??
            throw new InvalidOperationException(
                $"The gRPC client factory for '{typeof(TClient).FullName}' returned null.");
    }
}