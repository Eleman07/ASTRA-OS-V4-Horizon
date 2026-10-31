using System.Text.Json;
using AstraOS.Core;
using AstraOS.Core.Agents;

internal static class BenchmarkSuite
{
    public static string[] Create(AgentJobStore store)
    {
        if(File.Exists(Path.Combine(store.Root,"benchmark-jobs.json")))return JsonSerializer.Deserialize<string[]>(File.ReadAllText(Path.Combine(store.Root,"benchmark-jobs.json")))!;
        store.Settings.Put("runtime",new AgentRuntimeSettings{ContextSize=1_000_000,OutputTokens=0,SemanticSearch=false,UseGpu=false,PreferFreeCloud=true,CloudModel=Environment.GetEnvironmentVariable("ASTRA_CLOUD_MODEL")??"nvidia/nemotron-3-ultra-550b-a55b:free",FallbackToGpu=false,CloudModelAssignments=new(){{"engineering","cohere/north-mini-code:free"}}});
        var orbit=store.Enqueue("Prepare an engineering assessment of a 400 km Earth orbit using the actual numerical simulation source. Report circular reference speed rounded to 3 decimals in km/s and period rounded to a whole second; show the formulas, explain the ECI frame and km/s units, describe energy conservation evidence, and distinguish a two-body model from real flight validation. Cite [tool-simulation]. Do not invent sources.",new[]{"aerospace","engineering"});
        store.Update(orbit.Id,j=>{j.SimulationId="orbit";j.SimulationParameters=new(){{"alt",400},{"j2",0},{"duration",5600}};});
        var creator=store.Enqueue("Create a complete multi-format content pack: a 45-second video script, a 3-slide carousel and an accessibility checklist. Audience: university students. Brief: explain a student CubeSat project called ORBIT-42, without inventing flight results or performance. Include ORBIT-42 in both formats, captions and alt text. Use clear section headings and produce the actual copy, not a plan for writing it.",new[]{"creator"},"Provided brief: ORBIT-42 is a student CubeSat concept. No launch, mission success or performance has been measured. Budget: EUR 0.");
        var gaming=store.Enqueue("Design a controlled A/B gaming experiment comparing two graphics presets. Keep game version, resolution, scene and duration fixed, change one setting, run 3 repeats per condition, and define a decision rule. Available metrics are CPU, GPU utilization and RAM only. Explicitly state FPS: unmeasured. Do not invent benchmark results. Produce a practical worksheet and explain confounders.",new[]{"gaming","engineering"},"Supplied measurements are unavailable: this is an experiment-design task. No FPS, temperatures or latency were measured.");
        string[] ids={orbit.Id,creator.Id,gaming.Id};File.WriteAllText(Path.Combine(store.Root,"benchmark-jobs.json"),JsonSerializer.Serialize(ids));return ids;
    }
    public static bool Complete(AgentJobStore store)
    {
        string file=Path.Combine(store.Root,"benchmark-jobs.json");if(!File.Exists(file))return false;
        var jobs=JsonSerializer.Deserialize<string[]>(File.ReadAllText(file))!.Select(id=>store.Records.Get(id)!).ToArray();
        if(jobs.Any(j=>j.Status is "Queued" or "Running"))return false;
        var checks=new List<object>();bool passed=true;
        void Check(string name,bool ok){checks.Add(new{name,passed=ok});passed&=ok;}
        foreach(var job in jobs)Check("All specialist passes complete: "+job.Id,job.Status=="Completed"&&job.Steps.All(s=>s.Status=="Completed"));
        Check("Final evidence audits pass after correction",jobs.All(j=>!j.QualityNeedsReview&&AgentEngine.AuditPasses(j.QualityAudit)));
        string a=jobs[0].Steps.Last().Output,c=jobs[1].Steps.Last().Output,g=jobs[2].Steps.Last().Output;
        Check("Aerospace report preserves independently calculated speed and period",a.Contains("7.669")&&a.Contains("5554"));
        Check("Aerospace numeric evidence has a conservative RK4 energy error",jobs[0].Evidence.Any(e=>e.Id=="tool-simulation"&&e.Text.Contains("maxRelativeEnergyDrift")));
        Check("Aerospace final cites the actual simulation",a.Contains("[tool-simulation]"));
        Check("Creator delivers named multi-format content and accessibility",c.Contains("ORBIT-42")&&c.Contains("carousel",StringComparison.OrdinalIgnoreCase)&&(c.Contains("alt",StringComparison.OrdinalIgnoreCase)||c.Contains("caption",StringComparison.OrdinalIgnoreCase)));
        Check("Gaming preserves unmeasured FPS and controlled test scope",g.Contains("unmeasured",StringComparison.OrdinalIgnoreCase)&&g.Contains("resolution",StringComparison.OrdinalIgnoreCase)&&(g.Contains("repeat",StringComparison.OrdinalIgnoreCase)||g.Contains("three",StringComparison.OrdinalIgnoreCase)||g.Contains("3")));
        Check("No invented source IDs pass unnoticed",jobs.All(j=>!j.Steps.Last().Output.Contains("UNVERIFIED source IDs")));
        File.WriteAllText(Path.Combine(store.Root,"complex-benchmark.json"),JsonSerializer.Serialize(new{passed,backend=store.Configuration.PreferFreeCloud?"Free cloud only, zero price enforced; GPU fallback disabled for this comparison":"Local GPU inference only; no CPU or cloud fallback",checks,jobs=jobs.Select(j=>new{j.Id,j.Goal,j.Status,j.Error,final=j.Steps.Last().Output,evidence=j.Evidence.Select(e=>new{e.Id,e.Title,e.Location})})},new JsonSerializerOptions{WriteIndented=true}));
        return true;
    }
}
