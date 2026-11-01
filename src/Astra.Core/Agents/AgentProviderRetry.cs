using System.Net;

namespace AstraOS.Core.Agents;

/// <summary>A free provider is temporarily rate/quota limited after online routes have been tried.</summary>
public sealed class ProviderQuotaException : InvalidOperationException
{
    public DateTimeOffset? RetryAtUtc { get; }
    public ProviderQuotaException(string message, DateTimeOffset? retryAtUtc = null) : base(message) => RetryAtUtc = retryAtUtc;
}

/// <summary>Durable provider waiting is scheduling, separate from bounded crash repair.</summary>
public static class AgentProviderRetry
{
    public const string WaitingStatus = "WaitingForProvider";
    private const string LegacyQuotaMarker = "the free openrouter/provider request quota is temporarily exhausted";

    public static bool IsLegacyQuotaError(string? error) =>
        !string.IsNullOrWhiteSpace(error) && error.Contains(LegacyQuotaMarker, StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<Exception> Causes(Exception error)
    {
        var pending = new Stack<Exception>(); pending.Push(error);
        while (pending.TryPop(out var current))
        {
            yield return current;
            if (current is AggregateException aggregate) foreach (var inner in aggregate.InnerExceptions) pending.Push(inner);
            else if (current.InnerException is { } inner) pending.Push(inner);
        }
    }

    public static bool IsProviderQuota(Exception error) => Causes(error).Any(cause =>
        cause is ProviderQuotaException || cause is HttpRequestException { StatusCode: HttpStatusCode.TooManyRequests } || IsLegacyQuotaError(cause.Message));

    public static TimeSpan Backoff(int waitCount) => TimeSpan.FromMinutes(waitCount switch
    {
        <= 1 => 5,
        2 => 10,
        3 => 20,
        4 => 40,
        _ => 60
    });

    public static bool TrySchedule(AgentJobStore store, string jobId, Exception error, DateTimeOffset? utcNow = null)
    {
        ArgumentNullException.ThrowIfNull(store); ArgumentNullException.ThrowIfNull(error);
        if (!IsProviderQuota(error)) return false;
        DateTimeOffset now = (utcNow ?? DateTimeOffset.UtcNow).ToUniversalTime();
        DateTimeOffset? retryAfter = Causes(error).OfType<ProviderQuotaException>().Where(cause => cause.RetryAtUtc.HasValue)
            .Select(cause => cause.RetryAtUtc!.Value.ToUniversalTime()).OrderByDescending(value => value).Select(value => (DateTimeOffset?)value).FirstOrDefault();
        bool scheduled = false;
        store.Update(jobId, job =>
        {
            if (job.CancelRequested || job.Status is "Cancelled" or "Completed") return;
            // A concurrent supervisor must not advance an already durable wait or consume another attempt.
            if (job.Status == WaitingStatus && job.NextAttemptUtc is { } next && next > now) { scheduled = true; return; }
            job.ProviderWaitCount = job.ProviderWaitCount < int.MaxValue ? Math.Max(0, job.ProviderWaitCount) + 1 : int.MaxValue;
            DateTimeOffset due = now.Add(Backoff(job.ProviderWaitCount));
            if (retryAfter is { } supplied && supplied > due) due = supplied;
            job.Status = WaitingStatus;
            job.NextAttemptUtc = due;
            job.ProviderWaitingSinceUtc ??= now;
            job.ProviderWaitReason = error.Message;
            job.Error = error.Message;
            job.CompletedUtc = null;
            bool first = true;
            foreach (var step in job.Steps.Where(step => step.Status != "Completed"))
            { step.Status = first ? "Waiting for provider" : "Pending"; first = false; }
            scheduled = true;
        });
        return scheduled;
    }

    public static bool IsReady(AgentJob job, DateTimeOffset utcNow)
    {
        if (job.CancelRequested || job.DueUtc > utcNow) return false;
        if (job.Status == WaitingStatus) return job.NextAttemptUtc is { } retry && retry <= utcNow;
        return job.Status is "Queued" or "Running" && (!job.NextAttemptUtc.HasValue || job.NextAttemptUtc <= utcNow);
    }

    /// <summary>Convert only the exact historical quota failure; unrelated failures remain visible.</summary>
    public static int MigrateLegacyQuotaFailures(AgentJobStore store, DateTimeOffset? utcNow = null)
    {
        int migrated = 0;
        foreach (var job in store.Records.Enumerate().Where(job => job.Status == "Failed" && !job.CancelRequested && IsLegacyQuotaError(job.Error)))
            if (TrySchedule(store, job.Id, new ProviderQuotaException(job.Error), utcNow)) migrated++;
        return migrated;
    }

    /// <summary>An explicitly selected local provider is independent of a saved online quota wait.</summary>
    public static int ReleaseOnlineWaitsForLocalProvider(AgentJobStore store)
    {
        if (store.Configuration.PreferFreeCloud) return 0;
        int released = 0;
        foreach (var waiting in store.Records.Enumerate().Where(job => !job.CancelRequested && job.Status == WaitingStatus))
            store.Update(waiting.Id, job =>
            {
                if (job.CancelRequested || job.Status != WaitingStatus) return;
                job.Status = "Queued"; job.NextAttemptUtc = null; job.ProviderWaitingSinceUtc = null;
                job.ProviderWaitReason = "Online quota wait released after explicit selection of the local GPU provider.";
                job.Error = "";
                foreach (var step in job.Steps.Where(step => step.Status != "Completed")) step.Status = "Pending";
                released++;
            });
        return released;
    }
}
