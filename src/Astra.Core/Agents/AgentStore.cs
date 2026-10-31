using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AstraOS.Core.Agents;

/// <summary>One encrypted record per item; atomic replacement and a cross-process writer lock.</summary>
public sealed class EncryptedRecords<T>
{
    public string DirectoryPath { get; }
    public EncryptedRecords(string directory) { DirectoryPath = Path.GetFullPath(directory); }
    private string PathFor(string id) => Path.Combine(DirectoryPath, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(id))) + ".dpapi");
    public IDisposable Lock()
    {
        Directory.CreateDirectory(DirectoryPath);
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (true)
        {
            try { return new FileStream(Path.Combine(DirectoryPath, ".write-lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
            catch (IOException) when (DateTime.UtcNow < deadline) { Thread.Sleep(25); }
        }
    }
    public T? Get(string id) => File.Exists(PathFor(id)) ? Read(PathFor(id)) : default;
    private static T Read(string path)
    {
        byte[] plain = ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser);
        try { return JsonSerializer.Deserialize<T>(plain) ?? throw new InvalidDataException("Invalid encrypted record: " + Path.GetFileName(path)); }
        finally { CryptographicOperations.ZeroMemory(plain); }
    }
    public IEnumerable<T> Enumerate()
    {
        if (!Directory.Exists(DirectoryPath)) yield break;
        foreach (string file in Directory.EnumerateFiles(DirectoryPath, "*.dpapi"))
        {
            T record;
            try { record = Read(file); } catch (FileNotFoundException) { continue; }
            yield return record;
        }
    }
    // Caller holds Lock when a read-modify-write operation spans more than one call.
    public void Put(string id, T value)
    {
        Directory.CreateDirectory(DirectoryPath);
        byte[] plain = JsonSerializer.SerializeToUtf8Bytes(value);
        string target = PathFor(id), temp = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllBytes(temp, ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser)); File.Move(temp, target, true); }
        finally { CryptographicOperations.ZeroMemory(plain); if (File.Exists(temp)) File.Delete(temp); }
    }
    public void Delete(string id) => File.Delete(PathFor(id));
}

public sealed record AgentEvidence(string Id, string Title, string Text, string Location, string Kind);
public sealed record AgentImage(string Name,byte[] Pixels);
public sealed class AgentStep
{
    public string AgentId { get; set; } = "coordinator";
    public string Status { get; set; } = "Pending";
    public string Output { get; set; } = "";
    public DateTimeOffset? CompletedUtc { get; set; }
}
public sealed class AgentJob
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SeriesId { get; set; } = "";
    public string Goal { get; set; } = "";
    public string Context { get; set; } = "";
    public string ProjectRoot { get; set; } = "";
    public List<string> ImageIds { get; set; } = new();
    public bool AllowWeb { get; set; }
    public string SimulationId { get; set; } = "";
    public Dictionary<string, double> SimulationParameters { get; set; } = new();
    public string Status { get; set; } = "Queued";
    public bool CancelRequested { get; set; }
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset DueUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedUtc { get; set; }
    public double RepeatMinutes { get; set; }
    public bool RepeatScheduled { get; set; }
    public bool MemorySaved { get; set; }
    public bool VaultSaved { get; set; }
    public string VaultExportPath { get; set; } = "";
    public int RepairAttempts { get; set; }
    public string QualityAudit { get; set; } = "";
    public bool QualityNeedsReview { get; set; }
    public List<AgentStep> Steps { get; set; } = new();
    public List<AgentEvidence> Evidence { get; set; } = new();
    public string Error { get; set; } = "";
    public string Report => string.Join("\n\n", Steps.Where(s => s.Output.Length > 0).Select(s => "## " + AgentCatalog.Get(s.AgentId).Name + "\n\n" + s.Output))+ (QualityAudit.Length>0?"\n\n## Final evidence audit (model review)\n\n"+QualityAudit:"");
}
public sealed class AgentRuntimeSettings
{
    public bool Enabled { get; set; } = true;
    public string ModelPath { get; set; } = "";
    public uint ContextSize { get; set; } = 1000000;
    public int OutputTokens { get; set; }
    public int Threads { get; set; } = Math.Max(1, Environment.ProcessorCount - 1);
    public int IdleUnloadMinutes { get; set; } = 10;
    public bool SemanticSearch { get; set; }
    public bool UseGpu { get; set; }
    public bool UsePrecisionModel { get; set; } = true; // Legacy setting; all default inference uses the open GPU model.
    public bool PreferFreeCloud { get; set; } = true;
    public bool FallbackToGpu { get; set; }
    public string CloudModel { get; set; } = "nvidia/nemotron-3-ultra-550b-a55b:free";
    public Dictionary<string, string> CloudModelAssignments { get; set; } = new();
    public string VaultPath { get; set; } = "";
    public bool ExportToVault { get; set; } = true;
    public string ResearchArea { get; set; } = "STEM";
    public bool AutoRepair { get; set; } = true;
    public bool AutoLearn { get; set; } = true;
    public string VisionModelPath { get; set; } = "";
    public string VisionProjectorPath { get; set; } = "";
    public Dictionary<string,string[]> SkillAssignments { get; set; } = new();
}
public sealed class AgentJobStore
{
    public EncryptedRecords<AgentJob> Records { get; }
    public EncryptedRecords<AgentRuntimeSettings> Settings { get; }
    public EncryptedRecords<AgentImage> Images { get; }
    public string Root { get; }
    public AgentJobStore(string? root = null)
    {
        Root = root ?? Path.Combine(AstraPaths.UserDataRoot, "agents");
        Records = new(Path.Combine(Root, "jobs")); Settings = new(Path.Combine(Root, "settings"));
        Images = new(Path.Combine(Root,"images"));
    }
    public AgentRuntimeSettings Configuration => Settings.Get("runtime") ?? new();
    public AgentJob Enqueue(string goal, IEnumerable<string>? roles = null, string context = "", string project = "", bool web = false, double repeatMinutes = 0)
    {
        if (string.IsNullOrWhiteSpace(goal)) throw new ArgumentException("Describe the objective first.");
        if (!double.IsFinite(repeatMinutes) || repeatMinutes < 0) throw new ArgumentException("Invalid repeat interval.");
        string[] chosen = (roles ?? AgentCatalog.Route(goal)).Where(id => id is not "coordinator" and not "reviewer").Distinct().ToArray();
        foreach (string role in chosen) _ = AgentCatalog.Get(role);
        var job = new AgentJob { Goal = goal.Trim(), Context = context, ProjectRoot = project, AllowWeb = web, RepeatMinutes = repeatMinutes,
            Steps = new[] { "coordinator" }.Concat(chosen).Concat(new[] { "reviewer", "coordinator" }).Select(id => new AgentStep { AgentId = id }).ToList() };
        job.SeriesId = job.Id;
        using var guard = Records.Lock(); Records.Put(job.Id, job); return job;
    }
    public AgentJob Update(string id, Action<AgentJob> change)
    {
        using var guard = Records.Lock();
        var job = Records.Get(id) ?? throw new FileNotFoundException("Job no longer exists.");
        change(job); Records.Put(id, job); return job;
    }
    public void Cancel(string id)
    {
        using var guard = Records.Lock(); var job = Records.Get(id) ?? throw new FileNotFoundException();
        foreach (var item in Records.Enumerate().Where(j => j.SeriesId == job.SeriesId))
        { item.CancelRequested = true; item.RepeatMinutes = 0; if (item.Status is "Queued" or "Failed") item.Status = "Cancelled"; Records.Put(item.Id, item); }
    }
    public void ScheduleNext(AgentJob job)
    {
        if (job.Status != "Completed" || job.RepeatMinutes <= 0 || job.RepeatScheduled || job.CancelRequested) return;
        using var guard = Records.Lock();
        job = Records.Get(job.Id)!;
        if (job.RepeatScheduled || job.CancelRequested || job.RepeatMinutes <= 0) return;
        string nextId = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(job.Id + ":next")))[..32].ToLowerInvariant();
        if (Records.Get(nextId) is null)
        {
            var next = new AgentJob { Id = nextId, SeriesId = job.SeriesId, Goal = job.Goal, Context = job.Context,
                ProjectRoot = job.ProjectRoot, ImageIds = new(job.ImageIds), AllowWeb = job.AllowWeb, RepeatMinutes = job.RepeatMinutes,
                SimulationId = job.SimulationId, SimulationParameters = new(job.SimulationParameters),
                DueUtc = DateTimeOffset.UtcNow.AddMinutes(job.RepeatMinutes), Steps = job.Steps.Select(s => new AgentStep { AgentId = s.AgentId }).ToList() };
            Records.Put(next.Id, next);
        }
        job.RepeatScheduled = true; Records.Put(job.Id, job);
    }
}
