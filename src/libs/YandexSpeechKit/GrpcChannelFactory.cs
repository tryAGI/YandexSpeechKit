using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;

namespace YandexSpeechKit;

public static class GrpcChannelFactory
{
    public static GrpcChannel Create(string address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);

        return Create(new AutoSdkGrpcClientOptions
        {
            Address = address,
        });
    }

    public static GrpcChannel Create(AutoSdkGrpcClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Address);

        return GrpcChannel.ForAddress(options.Address, CreateChannelOptions(options));
    }

    private static GrpcChannelOptions CreateChannelOptions(AutoSdkGrpcClientOptions options)
    {
        var channelOptions = new GrpcChannelOptions();
        if (options.MaxRetryAttempts is > 1)
        {
            channelOptions.MaxRetryAttempts = options.MaxRetryAttempts.Value;
            channelOptions.ServiceConfig = new ServiceConfig
            {
                MethodConfigs =
                {
                    new MethodConfig
                    {
                        Names =
                        {
                            MethodName.Default,
                        },
                        RetryPolicy = new RetryPolicy
                        {
                            MaxAttempts = options.MaxRetryAttempts.Value,
                            InitialBackoff = options.RetryInitialBackoff,
                            MaxBackoff = options.RetryMaxBackoff,
                            BackoffMultiplier = options.RetryBackoffMultiplier,
                            RetryableStatusCodes =
                            {
                                StatusCode.Unavailable,
                                StatusCode.DeadlineExceeded,
                                StatusCode.Internal,
                            },
                        },
                    },
                },
            };
        }

        return channelOptions;
    }
}