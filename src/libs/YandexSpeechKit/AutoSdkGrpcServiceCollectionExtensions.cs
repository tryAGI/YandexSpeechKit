using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace YandexSpeechKit;

public static class AutoSdkGrpcServiceCollectionExtensions
{
    public static IServiceCollection AddAutoSdkGrpcClient<TClient>(
        this IServiceCollection services,
        Action<AutoSdkGrpcClientOptions> configure,
        Func<CallInvoker, TClient> clientFactory)
        where TClient : ClientBase<TClient>
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(clientFactory);

        var options = new AutoSdkGrpcClientOptions();
        configure(options);

        return services.AddAutoSdkGrpcClient(options, clientFactory);
    }

    public static IServiceCollection AddAutoSdkGrpcClient<TClient>(
        this IServiceCollection services,
        AutoSdkGrpcClientOptions options,
        Func<CallInvoker, TClient> clientFactory)
        where TClient : ClientBase<TClient>
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(clientFactory);

        services.AddSingleton(_ => new AutoSdkGrpcClient<TClient>(options.Clone(), clientFactory));
        services.AddSingleton(static provider => provider.GetRequiredService<AutoSdkGrpcClient<TClient>>().Client);

        return services;
    }
}