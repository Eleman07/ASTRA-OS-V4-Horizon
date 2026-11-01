using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AstraOS.Core;
using AstraOS.Core.Agents;

internal static class SimulationEvidenceArchiveTests
{
    public static void Run(string root,Action<bool,string> check)
    {
        string storeRoot=Path.Combine(root,"simulation-archives"),jobId=Guid.NewGuid().ToString("N");
        var definition=SimulationLab.Tools.Single(tool=>tool.Id=="orbit");
        var circular=OrbitalCalculator.Circular(6378.137+400,398600.4418);
        var parameters=new Dictionary<string,double>{{"alt",400},{"speed",circular.SpeedKmS},{"j2",0},{"inc",0},{"duration",circular.PeriodSeconds}};
        var result=SimulationLab.Run("orbit",parameters);
        double energy=result.Data.Rows[0][7]!.Value;
        var checks=new{referenceCircularSpeedKmS=circular.SpeedKmS,referenceCircularPeriodSeconds=circular.PeriodSeconds,maxRelativeEnergyDrift=result.Data.Rows.Max(row=>Math.Abs((row[7]!.Value-energy)/energy))};
        string full=JsonSerializer.Serialize(new{definition.Assumptions,parameters,result.Summary,checks,columns=result.Data.Columns,rows=result.Data.Rows,additionalFullMetadata=new string('m',180000)+"UNCLIPPED_FULL_METADATA_TAIL"});
        byte[] bytes=Encoding.UTF8.GetBytes(full);
        var saved=SimulationEvidenceArchive.Create(storeRoot,jobId,"orbit",definition.Name,full);
        check(File.ReadAllBytes(saved.Archive.Path).SequenceEqual(bytes)&&saved.Archive.Bytes==bytes.LongLength&&saved.Archive.Sha256==Convert.ToHexStringLower(SHA256.HashData(bytes)),"simulation archive preserves every original JSON byte, all table rows and long metadata with the actual full-file SHA-256");
        using(var inline=JsonDocument.Parse(saved.Evidence.Text))
        {
            var source=inline.RootElement;var metadata=source.GetProperty("fullTableArchive");
            check(saved.Evidence is{Id:"tool-simulation",Kind:"simulation"}&&!source.TryGetProperty("rows",out _)&&source.GetProperty("rowCount").GetInt32()==result.Data.Rows.Count&&metadata.GetProperty("allOriginalJsonPreserved").GetBoolean()&&!metadata.GetProperty("modelReadAllRows").GetBoolean()&&source.GetProperty("interpretation").GetString()!.Contains("not claim",StringComparison.Ordinal),"inline simulation evidence explicitly supplies checked metrics and metadata rather than pretending the model read every archived row");
            check(source.GetProperty("checks").GetProperty("referenceCircularSpeedKmS").GetDouble()==circular.SpeedKmS&&source.GetProperty("checks").GetProperty("maxRelativeEnergyDrift").GetDouble()==checks.maxRelativeEnergyDrift&&source.GetProperty("Assumptions").GetString()==definition.Assumptions&&source.GetProperty("Summary").GetString()==result.Summary&&source.GetProperty("columns").GetArrayLength()==result.Data.Columns.Count&&metadata.GetProperty("path").GetString()==saved.Archive.Path&&metadata.GetProperty("sha256").GetString()==saved.Archive.Sha256,"all assumptions, parameters, numerical checks, summary, column metadata and full archive references remain available inline");
        }
        check(SimulationEvidenceArchive.Verify(storeRoot,jobId,saved.Evidence)==saved.Archive,"archive verification checks the real full table, counts, hash and inline numerical metadata");
        string deterministicFacts=AgentEngine.AppendToolFacts("Draft",[saved.Evidence]);
        check(deterministicFacts.Contains("7.669",StringComparison.Ordinal)&&deterministicFacts.Contains("5554",StringComparison.Ordinal)&&deterministicFacts.Contains("[tool-simulation]",StringComparison.Ordinal),"archived-table evidence retains canonical speed and period for the final deterministic facts ledger");
        DateTime timestamp=File.GetLastWriteTimeUtc(saved.Archive.Path);
        var same=SimulationEvidenceArchive.Create(storeRoot,jobId,"orbit",definition.Name,full);
        check(same.Archive==saved.Archive&&File.GetLastWriteTimeUtc(saved.Archive.Path)==timestamp&&Directory.GetFiles(Path.GetDirectoryName(saved.Archive.Path)!).Length==1,"identical simulation evidence reuses only the verified original file without overwriting or leaving temporary files");
        var different=SimulationEvidenceArchive.Create(storeRoot,jobId,"orbit",definition.Name,full+"\n");
        check(different.Archive.Path!=saved.Archive.Path&&File.ReadAllBytes(saved.Archive.Path).SequenceEqual(bytes)&&File.ReadAllText(different.Archive.Path)==full+"\n","different complete JSON gets a new content-hash filename and preserves the previous archive");
        bool Rejected(Action operation){try{operation();return false;}catch(Exception ex)when(ex is InvalidDataException or ArgumentException or IOException or JsonException){return true;}}
        File.WriteAllText(saved.Archive.Path,"PRESERVE_MODIFIED_FILE");
        check(Rejected(()=>SimulationEvidenceArchive.Verify(storeRoot,jobId,saved.Evidence))&&Rejected(()=>SimulationEvidenceArchive.Create(storeRoot,jobId,"orbit",definition.Name,full))&&File.ReadAllText(saved.Archive.Path)=="PRESERVE_MODIFIED_FILE","modified content-addressed archives fail verification and are preserved rather than overwritten");
        File.WriteAllBytes(saved.Archive.Path,bytes);
        var changed=JsonNode.Parse(saved.Evidence.Text)!;changed["checks"]!["referenceCircularSpeedKmS"]=0;
        check(Rejected(()=>SimulationEvidenceArchive.Verify(storeRoot,jobId,saved.Evidence with{Text=changed.ToJsonString()})),"inline numerical claims must agree with the retained original JSON");
        changed=JsonNode.Parse(saved.Evidence.Text)!;changed["fullTableArchive"]!["path"]=Path.Combine(root,"outside.json");
        check(Rejected(()=>SimulationEvidenceArchive.Verify(storeRoot,jobId,saved.Evidence with{Text=changed.ToJsonString()})),"an archive reference cannot escape its private job evidence directory");
        foreach(string unsafeId in new[]{"../escape","..\\escape","C:\\escape",jobId+"/child","not-a-job"})
            check(Rejected(()=>SimulationEvidenceArchive.Create(storeRoot,unsafeId,"orbit",definition.Name,full)),"simulation archive rejects unsafe job path identity "+unsafeId);
        check(Rejected(()=>SimulationEvidenceArchive.Create(storeRoot,jobId,"../orbit",definition.Name,full))&&Rejected(()=>SimulationEvidenceArchive.Create(Path.Combine(root,"malformed-simulation"),jobId,"orbit",definition.Name,"{}"))&&!Directory.Exists(Path.Combine(root,"malformed-simulation")),"unknown simulation paths and malformed complete tables are rejected before creating destinations");
        using(var cancelled=new CancellationTokenSource())
        {
            cancelled.Cancel();bool stopped=false;try{SimulationEvidenceArchive.Create(Path.Combine(root,"cancelled-archive"),jobId,"orbit",definition.Name,full,cancelled.Token);}catch(OperationCanceledException){stopped=true;}
            check(stopped&&!Directory.Exists(Path.Combine(root,"cancelled-archive")),"cancelled simulation archival creates no partial file or destination");
        }
        string outside=Path.Combine(root,"simulation-link-target"),link=Path.Combine(root,"simulation-link");Directory.CreateDirectory(outside);File.WriteAllText(Path.Combine(outside,"sentinel.txt"),"PRESERVED");
        try
        {
            Directory.CreateSymbolicLink(link,outside);
            check(Rejected(()=>SimulationEvidenceArchive.Create(link,jobId,"orbit",definition.Name,full))&&Directory.Exists(link)&&File.ReadAllText(Path.Combine(outside,"sentinel.txt"))=="PRESERVED"&&!Directory.Exists(Path.Combine(outside,"Evidence")),"linked archive roots are rejected while preserving the link and its target");
            Directory.Delete(link);
            string external=Path.Combine(outside,"table.json");File.WriteAllText(external,"EXTERNAL_TABLE_PRESERVED");File.Delete(saved.Archive.Path);File.CreateSymbolicLink(saved.Archive.Path,external);
            check(Rejected(()=>SimulationEvidenceArchive.Create(storeRoot,jobId,"orbit",definition.Name,full))&&Rejected(()=>SimulationEvidenceArchive.Verify(storeRoot,jobId,saved.Evidence))&&File.ReadAllText(external)=="EXTERNAL_TABLE_PRESERVED","a linked content-hash file is never followed, overwritten or used as simulation evidence");
            File.Delete(saved.Archive.Path);File.WriteAllBytes(saved.Archive.Path,bytes);
        }
        catch(Exception ex)when(ex is UnauthorizedAccessException or PlatformNotSupportedException or IOException){Console.WriteLine("SKIP: simulation archive symlink fixture unavailable; production link checks remain enabled.");}
    }
}
