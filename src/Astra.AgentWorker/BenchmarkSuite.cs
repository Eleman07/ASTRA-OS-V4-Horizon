using System.Text.Json;
using System.Text.RegularExpressions;
using AstraOS.Core;
using AstraOS.Core.Agents;

internal static class BenchmarkSuite
{
    public static string[] Create(AgentJobStore store)
    {
        ApplyExplicitOverrides(store);
        string journal=Path.Combine(store.Root,"benchmark-jobs.json");
        if(File.Exists(journal))return ReadIds(journal);
        // Exercise the selected provider/model; only explicit benchmark environment variables alter settings.
        WriteJson(store,"benchmark-selection.json",Selection(store.Configuration));
        var orbit=store.Enqueue("Prepare an engineering assessment of a 400 km Earth orbit using the actual numerical simulation source. Report circular reference speed rounded to 3 decimals in km/s and period rounded to a whole second; show the formulas, explain the ECI frame and km/s units, describe energy conservation evidence, and distinguish a two-body model from real flight validation. Cite [tool-simulation]. Do not invent sources.",new[]{"aerospace","engineering"});
        store.Update(orbit.Id,j=>{j.SimulationId="orbit";j.SimulationParameters=new(){{"alt",400},{"j2",0},{"duration",5600}};});
        var creator=store.Enqueue("Create a complete multi-format content pack: a 45-second video script, a 3-slide carousel and an accessibility checklist. Audience: university students. Brief: explain a student CubeSat project called ORBIT-42, without inventing flight results or performance. Include ORBIT-42 in both formats, captions and alt text. Use clear section headings and produce the actual copy, not a plan for writing it.",new[]{"creator"},"Provided brief: ORBIT-42 is a student CubeSat concept. No launch, mission success or performance has been measured. Budget: EUR 0.");
        var gaming=store.Enqueue("Design a controlled A/B gaming experiment comparing two graphics presets. Keep game version, resolution, scene and duration fixed, change one setting, run 3 repeats per condition, and define a decision rule. Available metrics are CPU, GPU utilization and RAM only. Explicitly state FPS: unmeasured. Do not invent benchmark results. Produce a practical worksheet and explain confounders.",new[]{"gaming","engineering"},"Supplied measurements are unavailable: this is an experiment-design task. No FPS, temperatures or latency were measured.");
        string[] ids={orbit.Id,creator.Id,gaming.Id};WriteJson(store,"benchmark-jobs.json",ids);return ids;
    }
    public static bool Complete(AgentJobStore store)
    {
        string file=Path.Combine(store.Root,"benchmark-jobs.json");if(!File.Exists(file))return false;
        string[] ids;
        try{ids=ReadIds(file);}
        catch(Exception ex)when(ex is IOException or JsonException or InvalidDataException)
        {WriteJson(store,"complex-benchmark.json",new{status="FAILED",passed=false,modelTested=false,error=ex.Message});return true;}
        var records=ids.Select(id=>store.Records.Get(id)).ToArray();
        var jobs=records.OfType<AgentJob>().ToArray();
        if(jobs.Any(job=>AgentProviderRetry.IsReady(job,DateTimeOffset.UtcNow)))return false;
        bool blocked=jobs.Any(job=>job.Status==AgentProviderRetry.WaitingStatus||job.Status is "Queued" or "Running");
        var checks=new List<object>();bool passed=true;
        void Check(string name,bool ok){checks.Add(new{name,passed=ok});passed&=ok;}
        Check("Every benchmark job record is retained",jobs.Length==ids.Length);
        foreach(var job in jobs)Check("All specialist passes complete: "+job.Id,job.Status=="Completed"&&job.Steps.All(s=>s.Status=="Completed"));
        Check("Final evidence audits pass after correction",jobs.All(j=>!j.QualityNeedsReview&&AgentEngine.AuditPasses(j.QualityAudit)));
        string Final(int index)=>records[index]?.Steps.LastOrDefault()?.Output??"";
        string a=Final(0),c=Final(1),g=Final(2);
        Check("Aerospace report preserves independently calculated speed and period",a.Contains("7.669")&&a.Contains("5554"));
        Check("Aerospace numeric evidence retains its executed RK4 energy check",records[0]?.Evidence.Any(e=>e.Id=="tool-simulation"&&e.Text.Contains("maxRelativeEnergyDrift"))==true);
        Check("Aerospace final cites the actual simulation",a.Contains("[tool-simulation]"));
        Check("Creator delivers named multi-format content and accessibility",c.Contains("ORBIT-42")&&c.Contains("carousel",StringComparison.OrdinalIgnoreCase)&&(c.Contains("alt",StringComparison.OrdinalIgnoreCase)||c.Contains("caption",StringComparison.OrdinalIgnoreCase)));
        Check("Gaming preserves unmeasured FPS and controlled test scope",g.Contains("unmeasured",StringComparison.OrdinalIgnoreCase)&&g.Contains("resolution",StringComparison.OrdinalIgnoreCase)&&(g.Contains("repeat",StringComparison.OrdinalIgnoreCase)||g.Contains("three",StringComparison.OrdinalIgnoreCase)||g.Contains("3")));
        Check("No invented source IDs pass unnoticed",jobs.All(j=>!j.Steps.Last().Output.Contains("UNVERIFIED source IDs")));
        var configuration=store.Configuration;
        var observed=jobs.ToDictionary(job=>job.Id,job=>job.Evidence.Where(source=>source.Kind=="runtime"&&source.Id.StartsWith("tool-runtime-",StringComparison.Ordinal)).Select(Observe).ToArray());
        Check("Actual runtime evidence is recorded for each specialist",jobs.Length==ids.Length&&jobs.All(job=>job.Steps.Select(step=>step.AgentId).Distinct().All(role=>observed[job.Id].Any(record=>record.Role==role))));
        if(!configuration.PreferFreeCloud)
            Check("Local execution records positive GPU offload metrics for every specialist",jobs.Length==ids.Length&&jobs.All(job=>job.Steps.Select(step=>step.AgentId).Distinct().All(role=>observed[job.Id].Any(record=>record.Role==role&&record.OffloadedLayers>0))));
        else
            Check("Online execution records its actual provider for every specialist",jobs.Length==ids.Length&&jobs.All(job=>job.Steps.Select(step=>step.AgentId).Distinct().All(role=>observed[job.Id].Any(record=>record.Role==role&&record.Provider=="OpenRouter"))));
        string status=blocked?"BLOCKED":passed?"PASSED":"FAILED";
        object? initialSelection=null;string selectionFile=Path.Combine(store.Root,"benchmark-selection.json");
        if(File.Exists(selectionFile)){SafeFileOperations.NoLinks(selectionFile);using var selection=JsonDocument.Parse(File.ReadAllText(selectionFile));initialSelection=selection.RootElement.Clone();}
        var report=new
        {
            status,passed=passed&&!blocked,modelTested=observed.Values.Any(records=>records.Length>0),
            requestedSelection=Selection(configuration),initialSelection,
            localGpuOffloadObserved=observed.Values.SelectMany(records=>records).Any(record=>record.OffloadedLayers>0),
            observedProviders=observed.Values.SelectMany(records=>records).Select(record=>record.Provider).Distinct(),
            backendClaim="Requested settings and observed runtime metrics are reported separately. Completed text checks do not prove independent scientific or gaming measurements.",
            blockedReason=blocked?"One or more benchmark jobs are waiting for their provider or scheduled retry; this run stops without bypassing the saved schedule.":null,
            checks,utc=DateTimeOffset.UtcNow,
            jobs=jobs.Select(job=>new{job.Id,job.Goal,job.Status,job.Error,job.DueUtc,job.NextAttemptUtc,job.ProviderWaitCount,completedHandoffs=job.Steps.Count(step=>step.Status=="Completed"),final=job.Steps.LastOrDefault()?.Output??"",runtime=observed[job.Id],sourceKinds=job.Evidence.GroupBy(source=>source.Kind).ToDictionary(group=>group.Key,group=>group.Count())})
        };
        WriteJson(store,"complex-benchmark-"+DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N")+".json",report);
        WriteJson(store,"complex-benchmark.json",report);
        return true;
    }

    private sealed record RuntimeObservation(string Role,string Provider,int? OffloadedLayers,int? TotalLayers,string Record);
    private static RuntimeObservation Observe(AgentEvidence source)
    {
        var matches=Regex.Matches(source.Text,@"offloaded\s+(\d+)\s*/\s*(\d+)\s+layers",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant,TimeSpan.FromSeconds(1));
        var last=matches.LastOrDefault();int? offloaded=last is not null&&int.TryParse(last.Groups[1].Value,out int layers)?layers:null;
        int? total=last is not null&&int.TryParse(last.Groups[2].Value,out int count)?count:null;
        if(total is null or <=0||offloaded>total)offloaded=null;
        string provider=source.Text.Contains("Explicit online provider.",StringComparison.Ordinal)?"OpenRouter":offloaded>0?"Local GPU":"UNVERIFIED";
        return new(source.Id["tool-runtime-".Length..],provider,offloaded,total,source.Text);
    }
    private static object Selection(AgentRuntimeSettings settings)=>new
    {
        provider=settings.PreferFreeCloud?"OpenRouter":"Local GPU requested",localModel=Path.GetFileName(settings.ModelPath),runtime=Path.GetFileName(settings.RuntimePath),
        settings.UseGpu,settings.ContextSize,settings.OutputTokens,settings.CloudModel,cloudRoleAssignments=settings.CloudModelAssignments
    };
    private static string[] ReadIds(string path)
    {
        SafeFileOperations.NoLinks(path);
        var ids=JsonSerializer.Deserialize<string[]>(File.ReadAllText(path));
        if(ids is null||ids.Length!=3||ids.Distinct(StringComparer.Ordinal).Count()!=3||ids.Any(id=>!Guid.TryParseExact(id,"N",out _)))throw new InvalidDataException("The benchmark journal must retain exactly its three distinct job identities.");
        return ids;
    }
    private static void ApplyExplicitOverrides(AgentJobStore store)
    {
        var settings=store.Configuration;bool changed=false;
        if(Environment.GetEnvironmentVariable("ASTRA_BENCHMARK_PROVIDER") is{Length:>0} provider)
        {
            if(provider is not("local" or "openrouter"))throw new ArgumentException("ASTRA_BENCHMARK_PROVIDER must be local or openrouter.");
            settings.PreferFreeCloud=provider=="openrouter";settings.UseGpu=!settings.PreferFreeCloud;settings.FallbackToGpu=false;changed=true;
        }
        if(Environment.GetEnvironmentVariable("ASTRA_LOCAL_MODEL") is{Length:>0} model){settings.ModelPath=Path.GetFullPath(model);changed=true;}
        if(Environment.GetEnvironmentVariable("ASTRA_LOCAL_RUNTIME") is{Length:>0} runtime){settings.RuntimePath=Path.GetFullPath(runtime);changed=true;}
        if(Environment.GetEnvironmentVariable("ASTRA_LOCAL_CONTEXT") is{Length:>0} value)
        {if(!uint.TryParse(value,out uint context)||context==0)throw new ArgumentException("ASTRA_LOCAL_CONTEXT must be a positive context token count.");settings.ContextSize=context;changed=true;}
        if(changed)store.Settings.Put("runtime",settings);
    }
    private static void WriteJson(AgentJobStore store,string name,object value)
    {
        string path=SafeFileOperations.Child(store.Root,name);SafeFileOperations.NoLinks(path);
        string temporary=Path.Combine(store.Root,".benchmark-"+Guid.NewGuid().ToString("N")+".tmp");
        try{using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None))JsonSerializer.Serialize(stream,value,new JsonSerializerOptions{WriteIndented=true});SafeFileOperations.NoLinks(path);File.Move(temporary,path,true);}
        finally{if(File.Exists(temporary)){SafeFileOperations.NoLinks(temporary);File.Delete(temporary);}}
    }
}
