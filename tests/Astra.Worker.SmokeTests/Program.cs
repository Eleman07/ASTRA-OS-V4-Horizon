using System.Text.Json;
using AstraOS.Core.Agents;

string temporaryParent=Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(),"temp"));
string root=Path.Combine(temporaryParent,"benchmark-suite-tests-"+Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);int checks=0;
string[] variables=["ASTRA_BENCHMARK_PROVIDER","ASTRA_LOCAL_MODEL","ASTRA_LOCAL_RUNTIME","ASTRA_LOCAL_CONTEXT"];
var originalEnvironment=variables.ToDictionary(name=>name,Environment.GetEnvironmentVariable);
foreach(string name in variables)Environment.SetEnvironmentVariable(name,null);
void Check(bool passed,string name){if(!passed)throw new Exception(name);Console.WriteLine("PASS reporting fixture (no model calls): "+name);checks++;}
AgentJobStore Store(string name)
{
    var store=new AgentJobStore(Path.Combine(root,name));
    store.Settings.Put("runtime",new AgentRuntimeSettings{PreferFreeCloud=false,UseGpu=true,ContextSize=49152,OutputTokens=0,ModelPath=Path.Combine(root,"user-chosen.gguf"),RuntimePath=Path.Combine(root,"llama-server.exe"),CloudModel="openai/gpt-oss-20b:free",CloudModelAssignments=new(){{"engineering","cohere/north-mini-code:free"}}});
    return store;
}
JsonDocument Report(AgentJobStore store)=>JsonDocument.Parse(File.ReadAllText(Path.Combine(store.Root,"complex-benchmark.json")));
void CompleteFixtures(AgentJobStore store,string[] ids,string? runtime)
{
    for(int index=0;index<ids.Length;index++)
    {
        int fixture=index;
        store.Update(ids[index],job=>
        {
            job.Status="Completed";job.QualityNeedsReview=false;job.QualityAudit="{\"status\":\"PASS\",\"issues\":[]}";
            foreach(var step in job.Steps){step.Status="Completed";step.Output="Synthetic reporting fixture; no inference was performed.";}
            job.Steps.Last().Output=fixture switch{0=>"7.669 km/s and5554 s [tool-simulation]",1=>"ORBIT-42 carousel with alt text and captions",_=>"FPS:unmeasured. Fixed resolution;3 repeats per condition."};
            if(fixture==0)job.Evidence.Add(new("tool-simulation","Deterministic reporting fixture","{\"checks\":{\"maxRelativeEnergyDrift\":0}}","fixture","simulation"));
            job.Evidence.RemoveAll(source=>source.Kind=="runtime");
            if(runtime is not null)foreach(string role in job.Steps.Select(step=>step.AgentId).Distinct())job.Evidence.Add(new("tool-runtime-"+role,"Synthetic runtime ledger fixture",runtime,"fixture, not actual GPU execution","runtime"));
        });
    }
}
try
{
    var unchanged=Store("preserved-selection");string before=JsonSerializer.Serialize(unchanged.Configuration);var ids=BenchmarkSuite.Create(unchanged);
    Check(JsonSerializer.Serialize(unchanged.Configuration)==before,"creating the representative pack preserves provider, local model, context and cloud role assignments");
    unchanged.Update(ids[0],job=>{job.Status="Failed";job.Error="PRESERVE_FAILURE";job.Steps[0].Output="PRESERVE_PARTIAL_OUTPUT";});
    Check(BenchmarkSuite.Create(unchanged).SequenceEqual(ids)&&unchanged.Records.Get(ids[0]) is{Status:"Failed",Error:"PRESERVE_FAILURE"} retained&&retained.Steps[0].Output=="PRESERVE_PARTIAL_OUTPUT","reopening a benchmark reuses identities and preserves failed-job diagnostics/checkpoints");
    var waiting=Store("durable-wait");var waits=BenchmarkSuite.Create(waiting);DateTimeOffset retry=DateTimeOffset.UtcNow.AddHours(2);
    foreach(string id in waits)AgentProviderRetry.TrySchedule(waiting,id,new ProviderQuotaException("Synthetic shared provider quota",retry));
    Check(BenchmarkSuite.Complete(waiting),"a durable provider wait terminates the benchmark attempt without a manual resume or long polling");
    using(var report=Report(waiting))Check(report.RootElement.GetProperty("status").GetString()=="BLOCKED"&&!report.RootElement.GetProperty("passed").GetBoolean()&&!report.RootElement.GetProperty("modelTested").GetBoolean()&&waits.All(id=>waiting.Records.Get(id)!.NextAttemptUtc==retry),"waiting is BLOCKED, never PASSED or model-tested, and the provider retry schedule remains intact");
    var future=Store("future-jobs");var futureIds=BenchmarkSuite.Create(future);foreach(string id in futureIds)future.Update(id,job=>job.DueUtc=DateTimeOffset.UtcNow.AddHours(3));
    Check(BenchmarkSuite.Complete(future),"future-scheduled benchmark jobs stop the current attempt rather than looping until due");
    using(var report=Report(future))Check(report.RootElement.GetProperty("status").GetString()=="BLOCKED","future-scheduled jobs are reported explicitly as blocked");
    var ready=Store("pending-ready");BenchmarkSuite.Create(ready);
    Check(!BenchmarkSuite.Complete(ready)&&!File.Exists(Path.Combine(ready.Root,"complex-benchmark.json")),"ready queued jobs remain eligible for genuine execution and do not fabricate a completed report");
    var metrics=Store("runtime-metrics");var metricsIds=BenchmarkSuite.Create(metrics);CompleteFixtures(metrics,metricsIds,null);
    Check(BenchmarkSuite.Complete(metrics),"terminal reporting fixtures produce a report without inference");
    using(var report=Report(metrics))Check(report.RootElement.GetProperty("status").GetString()=="FAILED"&&!report.RootElement.GetProperty("localGpuOffloadObserved").GetBoolean(),"a local configuration flag alone cannot claim GPU execution or pass the benchmark");
    CompleteFixtures(metrics,metricsIds,"Explicit local GPU provider. Synthetic fixture diagnostics: offloaded 0/24 layers to GPU");BenchmarkSuite.Complete(metrics);
    using(var report=Report(metrics))Check(!report.RootElement.GetProperty("passed").GetBoolean()&&!report.RootElement.GetProperty("localGpuOffloadObserved").GetBoolean(),"zero offloaded layers remain unverified rather than being labeled GPU success");
    CompleteFixtures(metrics,metricsIds,"Explicit local GPU provider. Synthetic fixture diagnostics: offloaded 24/24 layers to GPU");BenchmarkSuite.Complete(metrics);
    using(var report=Report(metrics))Check(report.RootElement.GetProperty("passed").GetBoolean()&&report.RootElement.GetProperty("localGpuOffloadObserved").GetBoolean()&&report.RootElement.GetProperty("jobs")[0].GetProperty("runtime").EnumerateArray().All(record=>record.GetProperty("OffloadedLayers").GetInt32()==24),"positive per-role offload records are parsed as metrics; this deterministic fixture does not execute a GPU/model");
    Check(Directory.GetFiles(metrics.Root,"complex-benchmark-*.json").Length==3,"each terminal attempt preserves its historical report instead of deleting earlier failures");
    metrics.Records.Delete(metricsIds[1]);BenchmarkSuite.Complete(metrics);
    using(var report=Report(metrics))Check(report.RootElement.GetProperty("status").GetString()=="FAILED","missing encrypted job records fail reporting without inventing outputs");
    var overrides=Store("explicit-overrides");Environment.SetEnvironmentVariable("ASTRA_BENCHMARK_PROVIDER","openrouter");Environment.SetEnvironmentVariable("ASTRA_LOCAL_CONTEXT","65536");BenchmarkSuite.Create(overrides);
    Check(overrides.Configuration.PreferFreeCloud&&!overrides.Configuration.UseGpu&&overrides.Configuration.ContextSize==65536&&overrides.Configuration.CloudModel=="openai/gpt-oss-20b:free"&&overrides.Configuration.CloudModelAssignments["engineering"]=="cohere/north-mini-code:free","explicit provider/context overrides preserve the user's main and per-role cloud model choices");
    Environment.SetEnvironmentVariable("ASTRA_BENCHMARK_PROVIDER","local");Environment.SetEnvironmentVariable("ASTRA_LOCAL_MODEL",Path.Combine(root,"another-chosen.gguf"));Environment.SetEnvironmentVariable("ASTRA_LOCAL_RUNTIME",Path.Combine(root,"another-runtime","llama-server.exe"));BenchmarkSuite.Create(overrides);
    Check(!overrides.Configuration.PreferFreeCloud&&overrides.Configuration.UseGpu&&overrides.Configuration.ModelPath==Path.Combine(root,"another-chosen.gguf")&&overrides.Configuration.RuntimePath==Path.Combine(root,"another-runtime","llama-server.exe")&&overrides.Configuration.CloudModelAssignments.Count==1,"explicit local overrides change only requested fields and preserve all cloud assignments");
    string saved=JsonSerializer.Serialize(overrides.Configuration);Environment.SetEnvironmentVariable("ASTRA_LOCAL_CONTEXT","-1");bool rejected=false;try{BenchmarkSuite.Create(overrides);}catch(ArgumentException){rejected=true;}
    Check(rejected&&JsonSerializer.Serialize(overrides.Configuration)==saved,"invalid overrides fail before persisting a partially changed configuration");
    Console.WriteLine($"ALL {checks} WORKER BENCHMARK REPORTING CHECKS PASSED; no GPU or hosted inference was performed.");
}
finally
{
    foreach(var entry in originalEnvironment)Environment.SetEnvironmentVariable(entry.Key,entry.Value);
    if(!Path.GetFullPath(root).StartsWith(temporaryParent+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Refusing unexpected test cleanup target.");
    Directory.Delete(root,true);
}
