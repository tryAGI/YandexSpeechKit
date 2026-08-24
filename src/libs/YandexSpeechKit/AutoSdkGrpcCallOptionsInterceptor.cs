using System.Linq;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace YandexSpeechKit;

internal sealed class AutoSdkGrpcCallOptionsInterceptor : Interceptor
{
    private readonly AutoSdkGrpcClientOptions _options;

    public AutoSdkGrpcCallOptionsInterceptor(AutoSdkGrpcClientOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override TResponse BlockingUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        return continuation(request, CreateContext(context));
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        return continuation(request, CreateContext(context));
    }

    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        return continuation(request, CreateContext(context));
    }

    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        return continuation(CreateContext(context));
    }

    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        return continuation(CreateContext(context));
    }

    private ClientInterceptorContext<TRequest, TResponse> CreateContext<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context)
        where TRequest : class
        where TResponse : class
    {
        var callOptions = context.Options;
        var headers = CloneHeaders(callOptions.Headers);

        if (!string.IsNullOrWhiteSpace(_options.BearerToken) &&
            !headers.Any(static entry => string.Equals(entry.Key, "authorization", StringComparison.OrdinalIgnoreCase)))
        {
            headers.Add("authorization", $"Bearer {_options.BearerToken}");
        }

        if (!string.IsNullOrWhiteSpace(_options.ApiKey) &&
            !headers.Any(entry => string.Equals(entry.Key, _options.ApiKeyHeaderName, StringComparison.OrdinalIgnoreCase)))
        {
            headers.Add(_options.ApiKeyHeaderName, _options.ApiKey);
        }

        foreach (var header in _options.Headers)
        {
            if (!headers.Any(entry => string.Equals(entry.Key, header.Key, StringComparison.OrdinalIgnoreCase)))
            {
                headers.Add(header.Key, header.Value);
            }
        }

        if (headers.Count > 0)
        {
            callOptions = callOptions.WithHeaders(headers);
        }

        if (_options.Deadline is { } deadline &&
            callOptions.Deadline is null)
        {
            callOptions = callOptions.WithDeadline(DateTime.UtcNow.Add(deadline));
        }

        return new ClientInterceptorContext<TRequest, TResponse>(
            context.Method,
            context.Host,
            callOptions);
    }

    private static Metadata CloneHeaders(Metadata? source)
    {
        var headers = new Metadata();
        if (source == null)
        {
            return headers;
        }

        foreach (var entry in source)
        {
            if (entry.IsBinary)
            {
                headers.Add(entry.Key, entry.ValueBytes);
            }
            else
            {
                headers.Add(entry.Key, entry.Value);
            }
        }

        return headers;
    }
}