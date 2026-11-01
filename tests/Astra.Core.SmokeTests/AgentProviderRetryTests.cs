using System.Net;
using AstraOS.Core.Agents;

internal static class AgentProviderRetryTests
{
    public static async Task RunAsync(string root, Action<bool, string> check)
    {
        const string quota = "The free OpenRouter/provider request quota is temporarily exhausted. Check OpenRouter activity and retry after its reset. No paid or local fallback was used.";
        DateTimeOffset now = new(2026, 10, 31, 18, 30, 0, TimeSpan.Zero);
        var store = new AgentJobStore(Path.Combine(root, "provider-wait"));
        var job = store.Enqueue("Durable provider scheduling fixture", ["study"]);
        store.Update(job.Id, item => { item.DueUtc = now.AddMinutes(-1); item.Steps[0].Status = "Completed"; item.Steps[0].Output = "ORIGINAL_CHECKPOINT"; });
        check(AgentProviderRetry.Backoff(1) == TimeSpan.FromMinutes(5) && AgentProviderRetry.Backoff(2) == TimeSpan.FromMinutes(10) && AgentProviderRetry.Backoff(3) == TimeSpan.FromMinutes(20) && AgentProviderRetry.Backoff(4) == TimeSpan.FromMinutes(40) && AgentProviderRetry.Backoff(1000) == TimeSpan.FromHours(1), "provider backoff grows conservatively and caps at one hour");
        check(AgentProviderRetry.TrySchedule(store, job.Id, new ProviderQuotaException(quota, now.AddMinutes(42)), now), "a recognized quota condition schedules a durable provider wait");
        var waiting = store.Records.Get(job.Id)!;
        check(waiting.Status == AgentProviderRetry.WaitingStatus && waiting.NextAttemptUtc == now.AddMinutes(42) && waiting.ProviderWaitCount == 1 && waiting.ProviderWaitingSinceUtc == now && waiting.Error == quota, "waiting status and structured Retry-After are persisted");
        check(waiting.RepairAttempts == 0 && waiting.Steps[0] is { Status: "Completed", Output: "ORIGINAL_CHECKPOINT" } && waiting.Steps[1].Status == "Waiting for provider", "quota waiting preserves checkpoints and does not consume crash recovery attempts");
        check(store.NextReadyJob(now.AddMinutes(41)) is null && !AgentProviderRetry.IsReady(waiting, now.AddMinutes(41)), "the worker does not select a provider wait before its due time");
        check(store.NextReadyJob(now.AddMinutes(42))?.Id == job.Id, "the worker automatically selects a provider wait when its due time arrives");
        var restarted = new AgentJobStore(store.Root);
        var afterRestart = restarted.Records.Get(job.Id)!;
        check(afterRestart.NextAttemptUtc == waiting.NextAttemptUtc && afterRestart.ProviderWaitCount == 1 && afterRestart.Steps[0].Output == "ORIGINAL_CHECKPOINT" && restarted.NextReadyJob(now.AddMinutes(41)) is null, "process restart retains waiting schedule and completed handoffs without an early retry");
        AgentProviderRetry.TrySchedule(restarted, job.Id, new ProviderQuotaException(quota), now.AddMinutes(1));
        check(restarted.Records.Get(job.Id) is { ProviderWaitCount: 1 } unchanged && unchanged.NextAttemptUtc == now.AddMinutes(42), "a duplicate supervisor observation does not increment or advance a durable wait");
        restarted.Cancel(job.Id);
        check(restarted.Records.Get(job.Id) is { Status: "Cancelled", CancelRequested: true, NextAttemptUtc: null } && restarted.NextReadyJob(now.AddDays(2)) is null && !AgentProviderRetry.TrySchedule(restarted, job.Id, new ProviderQuotaException(quota), now.AddDays(2)), "cancellation removes the automatic retry and cannot be undone by a later quota event");

        var longRetryStore = new AgentJobStore(Path.Combine(root, "provider-retry-header"));
        var longRetry = longRetryStore.Enqueue("Server retry-after fixture", ["study"]);
        longRetryStore.Update(longRetry.Id, item => item.DueUtc = now);
        AgentProviderRetry.TrySchedule(longRetryStore, longRetry.Id, new AggregateException(new IOException("transport wrapper", new ProviderQuotaException(quota, now.AddHours(3)))), now);
        check(longRetryStore.Records.Get(longRetry.Id)!.NextAttemptUtc == now.AddHours(3) && longRetryStore.NextReadyJob(now.AddHours(2)) is null, "a provider Retry-After beyond one hour is honored through wrapped exceptions");
        check(AgentProviderRetry.IsProviderQuota(new HttpRequestException("rate limited", null, HttpStatusCode.TooManyRequests)) && !AgentProviderRetry.IsProviderQuota(new IOException("Disk quota exceeded")) && !AgentProviderRetry.IsProviderQuota(new HttpRequestException("service unavailable", null, HttpStatusCode.ServiceUnavailable)), "quota classification accepts typed HTTP 429 and rejects unrelated quota or server failures");

        var migrationStore = new AgentJobStore(Path.Combine(root, "provider-wait-migration"));
        var legacy = migrationStore.Enqueue("Old quota fixture", ["study"]);
        migrationStore.Update(legacy.Id, item => { item.Status = "Failed"; item.Error = quota; item.RepairAttempts = 3; item.DueUtc = now.AddMinutes(-1); item.Steps[0].Status = "Completed"; item.Steps[0].Output = "LEGACY_CHECKPOINT"; });
        var unrelated = migrationStore.Enqueue("Permission fixture", ["study"]);
        migrationStore.Update(unrelated.Id, item => { item.Status = "Failed"; item.Error = "Access to the path is denied."; });
        check(AgentProviderRetry.MigrateLegacyQuotaFailures(migrationStore, now) == 1 && AgentProviderRetry.MigrateLegacyQuotaFailures(migrationStore, now) == 0, "legacy migration recognizes only specific old quota failures and is idempotent");
        check(migrationStore.Records.Get(legacy.Id) is { Status: AgentProviderRetry.WaitingStatus, ProviderWaitCount: 1, RepairAttempts: 3 } migrated && migrated.NextAttemptUtc == now.AddMinutes(5) && migrated.Steps[0].Output == "LEGACY_CHECKPOINT" && migrationStore.Records.Get(unrelated.Id)!.Status == "Failed", "legacy quota jobs recover automatically despite exhausted old crash budget while unrelated failures remain visible");
        migrationStore.Settings.Put("runtime", new AgentRuntimeSettings { PreferFreeCloud = true });
        check(AgentProviderRetry.ReleaseOnlineWaitsForLocalProvider(migrationStore) == 0 && migrationStore.Records.Get(legacy.Id)!.Status == AgentProviderRetry.WaitingStatus, "online mode never bypasses the provider's persisted retry schedule");
        migrationStore.Settings.Put("runtime", new AgentRuntimeSettings { PreferFreeCloud = false, UseGpu = true });
        check(AgentProviderRetry.ReleaseOnlineWaitsForLocalProvider(migrationStore) == 1 && migrationStore.Records.Get(legacy.Id) is { Status: "Queued", NextAttemptUtc: null, ProviderWaitCount: 1, RepairAttempts: 3 } local && local.Steps[0].Output == "LEGACY_CHECKPOINT" && local.Steps[1].Status == "Pending" && migrationStore.Records.Get(unrelated.Id)!.Status == "Failed", "explicit local selection releases only online waiting without losing checkpoints or unrelated failures");
        check(AgentProviderRetry.ReleaseOnlineWaitsForLocalProvider(restarted) == 0 && restarted.Records.Get(job.Id)!.Status == "Cancelled", "switching to local never revives cancelled provider jobs");
        var noRepairStore = new AgentJobStore(Path.Combine(root, "provider-wait-without-crash-repair"));
        noRepairStore.Settings.Put("runtime", new AgentRuntimeSettings { AutoRepair = false });
        var noRepair = noRepairStore.Enqueue("Provider scheduling is not crash repair", ["study"]);
        noRepairStore.Update(noRepair.Id, item => { item.Status = "Failed"; item.Error = quota; });
        check(AgentRecoveryPolicy.TryRecover(noRepairStore, noRepairStore.Records.Get(noRepair.Id)!) && noRepairStore.Records.Get(noRepair.Id) is { Status: AgentProviderRetry.WaitingStatus, RepairAttempts: 0 }, "automatic provider scheduling is independent of the optional crash repair switch");

        var engineStore = new AgentJobStore(Path.Combine(root, "provider-engine"));
        var engineJob = engineStore.Enqueue("Checkpointed provider quota fixture", ["study"]);
        int firstCalls = 0, gathers = 0;
        var tools = new FixtureTools(() => gathers++);
        var limitedEngine = new AgentEngine(engineStore, tools, (_, _, _) =>
        {
            if (++firstCalls == 2) throw new ProviderQuotaException(quota, DateTimeOffset.UtcNow.AddHours(1));
            return Task.FromResult("COMPLETED_BEFORE_QUOTA");
        });
        await limitedEngine.RunAsync(engineJob.Id, default);
        var engineWaiting = engineStore.Records.Get(engineJob.Id)!;
        check(engineWaiting is { Status: AgentProviderRetry.WaitingStatus, ProviderWaitCount: 1, RepairAttempts: 0 } && engineWaiting.Steps[0].Output == "COMPLETED_BEFORE_QUOTA", "an actual engine quota exception produces waiting instead of terminal failure");
        int resumedCalls = 0;
        var resumedEngine = new AgentEngine(engineStore, tools, (system, _, _) => { resumedCalls++; return Task.FromResult(system.Contains("FINAL_EVIDENCE_AUDIT") ? "{\"status\":\"PASS\",\"issues\":[]}" : "AUTOMATICALLY_CONTINUED"); });
        int gathersBefore = gathers;
        await resumedEngine.RunAsync(engineJob.Id, default);
        check(resumedCalls == 0 && gathers == gathersBefore && engineStore.Records.Get(engineJob.Id)!.Status == AgentProviderRetry.WaitingStatus, "even a direct engine invocation cannot bypass a future provider retry");
        engineStore.Update(engineJob.Id, item => { item.NextAttemptUtc = DateTimeOffset.UtcNow.AddMinutes(-1); item.DueUtc = DateTimeOffset.UtcNow.AddMinutes(-2); });
        check(engineStore.NextReadyJob(DateTimeOffset.UtcNow)?.Id == engineJob.Id, "a persisted job becomes eligible without any manual resume transition");
        await resumedEngine.RunAsync(engineJob.Id, default);
        var completed = engineStore.Records.Get(engineJob.Id)!;
        check(completed.Status == "Completed" && completed.NextAttemptUtc is null && completed.ProviderWaitingSinceUtc is null && completed.Steps.All(step => step.Status == "Completed") && completed.Steps[0].Output == "COMPLETED_BEFORE_QUOTA" && resumedCalls == 4 && completed.RepairAttempts == 0, "automatic retry completes remaining handoffs and skips the saved checkpoint");
        var deniedJob = engineStore.Enqueue("Non-quota failure fixture", ["study"]);
        var deniedEngine = new AgentEngine(engineStore, tools, (_, _, _) => throw new UnauthorizedAccessException("fixture permission failure"));
        await deniedEngine.RunAsync(deniedJob.Id, default);
        check(engineStore.Records.Get(deniedJob.Id) is { Status: "Failed", NextAttemptUtc: null, ProviderWaitCount: 0 }, "non-quota failures retain their ordinary failure handling");
        var draftStore=new AgentJobStore(Path.Combine(root,"provider-audit-checkpoint"));
        var draftJob=draftStore.Enqueue("Preserve the full generated project if its final audit is quota blocked",["study"]);
        string draft=new string('p',120000)+"GENERATED_PROJECT_TAIL";
        var auditLimited=new AgentEngine(draftStore,tools,(system,input,ct)=>system.Contains("FINAL_EVIDENCE_AUDIT",StringComparison.Ordinal)?throw new ProviderQuotaException(quota):Task.FromResult(draft));
        await auditLimited.RunAsync(draftJob.Id,default);
        check(draftStore.Records.Get(draftJob.Id) is {Status:AgentProviderRetry.WaitingStatus} draftWaiting&&draftWaiting.Steps.Last().Status!="Completed"&&draftWaiting.Steps.Last().Output==draft&&draftWaiting.Steps.Take(3).All(step=>step.Status=="Completed"),"a final-audit outage preserves the complete generated draft without marking its unreviewed handoff completed");
    }

    private sealed class FixtureTools(Action gathered) : IAgentTools
    {
        public Task<IReadOnlyList<AgentEvidence>> GatherAsync(Specialist agent, AgentJob job, CancellationToken ct)
        { gathered(); return Task.FromResult<IReadOnlyList<AgentEvidence>>([new("note-wait-fixture", "Local test source", "fixture evidence", "fixture", "memory")]); }
    }
}
