namespace AstraOS.Core.Agents;

/// <summary>Bounded recovery for recognized transient worker/model failures.</summary>
public static class AgentRecoveryPolicy
{
    private static readonly string[] TransientMarkers =
    {
        "timeout", "timed out", "connection", "503", "temporarily", "checksum",
        "terminato prima", "incomplete model chunk"
    };

    public static bool TryRecover(AgentJobStore store, AgentJob failed)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(failed);
        if (!store.Configuration.AutoRepair) return false;

        string error = failed.Error.ToLowerInvariant();
        if (!TransientMarkers.Any(error.Contains)) return false;
        var current = store.Records.Get(failed.Id);
        if (current is null || current.Status != "Failed" || current.CancelRequested || current.RepairAttempts >= 3) return false;

        int nextAttempt = current.RepairAttempts + 1;
        var recovered = store.Update(current.Id, job =>
        {
            // Recheck after acquiring the record lock so a cancellation or another
            // supervisor update cannot be undone by this retry.
            if (job.Status != "Failed" || job.CancelRequested || job.RepairAttempts >= 3) return;
            job.Status = "Queued";
            job.RepairAttempts++;
            job.DueUtc = DateTimeOffset.UtcNow.AddSeconds(15 * job.RepairAttempts);
        });
        if (recovered.Status != "Queued" || recovered.RepairAttempts != nextAttempt) return false;
        if (store.Configuration.AutoLearn) new AgentLearning(store.Root).Observe("coordinator", "recovery", recovered.Id);
        return true;
    }
}
