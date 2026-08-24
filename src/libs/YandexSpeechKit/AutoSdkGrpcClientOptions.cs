using System.Collections.Generic;

namespace YandexSpeechKit;

public sealed class AutoSdkGrpcClientOptions
{
    public string Address { get; set; } = string.Empty;

    public string? BearerToken { get; set; }

    public string? ApiKey { get; set; }

    public string ApiKeyHeaderName { get; set; } = "x-api-key";

    public TimeSpan? Deadline { get; set; }

    public int? MaxRetryAttempts { get; set; }

    public TimeSpan RetryInitialBackoff { get; set; } = TimeSpan.FromMilliseconds(200);

    public TimeSpan RetryMaxBackoff { get; set; } = TimeSpan.FromSeconds(2);

    public double RetryBackoffMultiplier { get; set; } = 2.0d;

    public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public AutoSdkGrpcClientOptions AddHeader(string name, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(value);

        Headers[name] = value;
        return this;
    }

    public AutoSdkGrpcClientOptions UseBearerToken(string bearerToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bearerToken);

        BearerToken = bearerToken;
        return this;
    }

    public AutoSdkGrpcClientOptions UseApiKey(string apiKey, string headerName = "x-api-key")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(headerName);

        ApiKey = apiKey;
        ApiKeyHeaderName = headerName;
        return this;
    }

    public AutoSdkGrpcClientOptions WithDefaultDeadline(TimeSpan deadline)
    {
        if (deadline <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(deadline), "Deadline must be greater than zero.");
        }

        Deadline = deadline;
        return this;
    }

    public AutoSdkGrpcClientOptions WithRetry(
        int maxAttempts = 3,
        TimeSpan? initialBackoff = null,
        TimeSpan? maxBackoff = null,
        double? backoffMultiplier = null)
    {
        if (maxAttempts < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Retry attempts must be at least 2.");
        }

        if (initialBackoff.HasValue &&
            initialBackoff.Value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(initialBackoff), "Initial backoff must be greater than zero.");
        }

        if (maxBackoff.HasValue &&
            maxBackoff.Value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(maxBackoff), "Max backoff must be greater than zero.");
        }

        if (backoffMultiplier is <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(backoffMultiplier), "Backoff multiplier must be greater than zero.");
        }

        MaxRetryAttempts = maxAttempts;
        RetryInitialBackoff = initialBackoff ?? TimeSpan.FromMilliseconds(200);
        RetryMaxBackoff = maxBackoff ?? TimeSpan.FromSeconds(2);
        RetryBackoffMultiplier = backoffMultiplier ?? 2.0d;
        return this;
    }

    public AutoSdkGrpcClientOptions Clone()
    {
        var clone = new AutoSdkGrpcClientOptions
        {
            Address = Address,
            BearerToken = BearerToken,
            ApiKey = ApiKey,
            ApiKeyHeaderName = ApiKeyHeaderName,
            Deadline = Deadline,
            MaxRetryAttempts = MaxRetryAttempts,
            RetryInitialBackoff = RetryInitialBackoff,
            RetryMaxBackoff = RetryMaxBackoff,
            RetryBackoffMultiplier = RetryBackoffMultiplier,
        };

        foreach (var header in Headers)
        {
            clone.Headers[header.Key] = header.Value;
        }

        return clone;
    }
}