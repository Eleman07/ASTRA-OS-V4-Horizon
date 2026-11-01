using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AstraOS.Core.Agents;

public sealed record SimulationArchiveInfo(string Path,string Sha256,long Bytes,int RowCount,int ColumnCount);
public sealed record ArchivedSimulationEvidence(AgentEvidence Evidence,SimulationArchiveInfo Archive);

/// <summary>Retain every deterministic table value while explicitly supplying the model checked metrics and an archive reference.</summary>
public static class SimulationEvidenceArchive
{
    private static readonly UTF8Encoding Utf8=new(false,true);
    private static readonly string[] InlineFields=["Assumptions","parameters","Summary","checks","columns"];

    public static ArchivedSimulationEvidence Create(string storeRoot,string jobId,string simulationId,string title,string fullJson,CancellationToken ct=default)
    {
        ct.ThrowIfCancellationRequested();
        ValidateIds(jobId,simulationId);
        using var original=JsonDocument.Parse(fullJson);
        ValidateTable(original.RootElement);
        byte[] bytes=Utf8.GetBytes(fullJson);
        string digest=Convert.ToHexStringLower(SHA256.HashData(bytes));
        string directory=ArchiveDirectory(storeRoot,jobId);
        string target=Path.Combine(directory,simulationId+"-"+digest+".json");
        SafeFileOperations.NoLinks(directory);SafeFileOperations.NoLinks(target);
        Directory.CreateDirectory(directory);SafeFileOperations.NoLinks(directory);
        if(File.Exists(target))VerifyBytes(target,digest,bytes.LongLength,ct);
        else
        {
            string stage=Path.Combine(directory,".simulation-"+Guid.NewGuid().ToString("N")+".tmp");
            bool created=false;
            try
            {
                using(var output=new FileStream(stage,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                {created=true;output.Write(bytes);output.Flush(flushToDisk:true);}
                ct.ThrowIfCancellationRequested();SafeFileOperations.NoLinks(directory);SafeFileOperations.NoLinks(target);
                try{File.Move(stage,target,overwrite:false);created=false;}
                catch(IOException) when(File.Exists(target)){VerifyBytes(target,digest,bytes.LongLength,ct);}
            }
            finally
            {
                if(created)
                {
                    try{SafeFileOperations.NoLinks(stage);if(File.Exists(stage))File.Delete(stage);}
                    catch(IOException){}catch(UnauthorizedAccessException){}
                }
            }
        }
        var archive=new SimulationArchiveInfo(target,digest,bytes.LongLength,original.RootElement.GetProperty("rows").GetArrayLength(),original.RootElement.GetProperty("columns").GetArrayLength());
        var source=original.RootElement;
        string representation=JsonSerializer.Serialize(new
        {
            schemaVersion=1,simulationId,
            representation="Computed checks, assumptions, parameters, summary and column metadata; the complete table is archived separately, not inlined into the model prompt.",
            Assumptions=source.GetProperty("Assumptions"),parameters=source.GetProperty("parameters"),Summary=source.GetProperty("Summary"),checks=source.GetProperty("checks"),columns=source.GetProperty("columns"),
            rowCount=archive.RowCount,
            fullTableArchive=new{path=archive.Path,sha256=archive.Sha256,bytes=archive.Bytes,rowCount=archive.RowCount,columnCount=archive.ColumnCount,allOriginalJsonPreserved=true,modelReadAllRows=false},
            interpretation="The model receives the inline computed checks and metadata only. The full original JSON, including every row, is retained at the archive path with SHA-256 verification. Do not claim to have read every archived row or executed model-generated software."
        });
        return new(new("tool-simulation",title,representation,"SimulationLab:"+simulationId+"; full numerical archive: "+target,"simulation"),archive);
    }

    /// <summary>Verify archive containment, bytes, every table row and agreement with the inline numerical metadata.</summary>
    public static SimulationArchiveInfo Verify(string storeRoot,string jobId,AgentEvidence evidence,CancellationToken ct=default)
    {
        ct.ThrowIfCancellationRequested();
        if(evidence.Kind!="simulation")throw new InvalidDataException("The evidence does not identify a simulation archive.");
        using var inline=JsonDocument.Parse(evidence.Text);
        var root=inline.RootElement;
        string simulationId=root.GetProperty("simulationId").GetString()??throw new InvalidDataException("Missing simulation identity.");
        ValidateIds(jobId,simulationId);
        var metadata=root.GetProperty("fullTableArchive");
        string digest=metadata.GetProperty("sha256").GetString()??"";
        if(digest.Length!=64||digest.Any(character=>!char.IsAsciiHexDigit(character)))throw new InvalidDataException("Invalid simulation archive SHA-256.");
        string expected=Path.Combine(ArchiveDirectory(storeRoot,jobId),simulationId+"-"+digest.ToLowerInvariant()+".json");
        string path=metadata.GetProperty("path").GetString()??"";
        if(!Path.IsPathFullyQualified(path)||!Path.GetFullPath(path).Equals(expected,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("The simulation archive escapes its private job evidence directory.");
        long bytes=metadata.GetProperty("bytes").GetInt64();
        if(bytes<0||!metadata.GetProperty("allOriginalJsonPreserved").GetBoolean()||metadata.GetProperty("modelReadAllRows").GetBoolean())throw new InvalidDataException("The simulation evidence archive representation is inconsistent.");
        VerifyBytes(path,digest,bytes,ct);
        using var full=JsonDocument.Parse(File.ReadAllBytes(path));
        ValidateTable(full.RootElement);
        int rows=full.RootElement.GetProperty("rows").GetArrayLength(),columns=full.RootElement.GetProperty("columns").GetArrayLength();
        if(rows!=metadata.GetProperty("rowCount").GetInt32()||rows!=root.GetProperty("rowCount").GetInt32()||columns!=metadata.GetProperty("columnCount").GetInt32())throw new InvalidDataException("The simulation archive row/column counts disagree with the inline metadata.");
        foreach(string field in InlineFields)
            if(!JsonElement.DeepEquals(full.RootElement.GetProperty(field),root.GetProperty(field)))throw new InvalidDataException("The inline simulation metadata differs from its original archive: "+field);
        return new(path,digest.ToLowerInvariant(),bytes,rows,columns);
    }

    private static void ValidateTable(JsonElement source)
    {
        if(source.ValueKind!=JsonValueKind.Object||!source.TryGetProperty("rows",out var rows)||rows.ValueKind!=JsonValueKind.Array||!source.TryGetProperty("columns",out var columns)||columns.ValueKind!=JsonValueKind.Array)throw new InvalidDataException("A simulation archive requires the complete rows and columns arrays.");
        foreach(string field in InlineFields)if(!source.TryGetProperty(field,out _))throw new InvalidDataException("The complete simulation JSON is missing "+field+".");
        foreach(var row in rows.EnumerateArray())if(row.ValueKind!=JsonValueKind.Array||row.GetArrayLength()!=columns.GetArrayLength())throw new InvalidDataException("A simulation table row does not match its column metadata.");
    }
    private static void ValidateIds(string jobId,string simulationId)
    {
        if(!Guid.TryParseExact(jobId,"N",out _)||!SimulationLab.Tools.Any(tool=>tool.Id==simulationId))throw new InvalidDataException("Only a valid job identity and a known deterministic simulation can select an evidence archive path.");
    }
    private static string ArchiveDirectory(string storeRoot,string jobId)
    {
        string root=Path.TrimEndingDirectorySeparator(Path.GetFullPath(storeRoot));SafeFileOperations.NoLinks(root);
        string directory=Path.GetFullPath(Path.Combine(root,"Evidence",jobId));
        if(!directory.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("The simulation archive is outside the job store.");
        SafeFileOperations.NoLinks(directory);return directory;
    }
    private static void VerifyBytes(string path,string digest,long bytes,CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();SafeFileOperations.NoLinks(path);
        using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
        if(stream.Length!=bytes||!Convert.ToHexString(SHA256.HashData(stream)).Equals(digest,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("The retained simulation archive is incomplete or modified; existing files were not overwritten.");
    }
}
