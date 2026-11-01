using System.Security.Cryptography;
using System.Text.Json;
using AstraOS.Core;
using AstraOS.Core.Agents;

namespace AstraOS.Desktop.Ai;
public static class BundledAi
{
    private const string WhisperDigest="be07e048e1e599ad46341c8d2a135645097a538221678b7acdd1b1919c6e1b21";
    private const string WhisperSmallDigest="1be3a9b2063867b937e64e2ec7483364a79917e157fa98c5d94b5c1fffea987b";
    public static string DirectoryPath=>Path.Combine(AppContext.BaseDirectory,"Ai","components");
    public static string ModelPath=>new AgentJobStore().Configuration.ModelPath;
    public static string RuntimePath=>Path.Combine(AppContext.BaseDirectory,"LLamaSharp.dll");
    public static bool IsLegacyBundledRuntime(string path)
    {
        if(string.IsNullOrWhiteSpace(path))return false;
        string full=Path.GetFullPath(path.Trim().Trim('"'));
        // A previous portable folder may have moved. Only recognize the old
        // bundled layout; user-selected external runtimes keep their setting.
        return full.EndsWith(Path.Combine("Ai","components","llama-server.exe"),StringComparison.OrdinalIgnoreCase)||full.Equals(RuntimePath,StringComparison.OrdinalIgnoreCase);
    }
    public static async Task VerifyAsync(CancellationToken ct)
    {
        string path=ModelPath;
        if(string.IsNullOrWhiteSpace(path))throw new InvalidOperationException("Select a verified local GPU model in ASTRA model settings. The legacy bundled text model is no longer used.");
        await LocalModelCatalog.VerifyTextModelAsync(path,ct);
    }

    public static Task VerifyRuntimeAsync(CancellationToken ct)=>Task.FromException(new InvalidOperationException("The legacy embedded CPU text runtime is disabled. Select the verified GPU llama-server runtime in ASTRA model settings."));

    public static async Task<string> VerifyWhisperAsync(CancellationToken ct,string? modelId=null)
    {
        string selected=modelId??new AgentJobStore().Configuration.SpeechModelId;
        string name=selected switch{"small"=>"ggml-small.bin","tiny"=>"ggml-tiny.bin",_=>throw new InvalidDataException("Select OpenAI Whisper small for precision or tiny for faster local transcription. Unknown speech weights are not enabled.")};
        string digest=selected=="small"?WhisperSmallDigest:WhisperDigest;
        string manifest=Path.Combine(DirectoryPath,"manifest.json");
        if(!File.Exists(manifest))throw MissingComponent("manifest.json");
        SafeFileOperations.NoLinks(manifest);
        using var document=JsonDocument.Parse(await File.ReadAllTextAsync(manifest,ct));
        if(!document.RootElement.TryGetProperty("files",out var files)||!files.TryGetProperty(name,out var record)||record.GetString() is not string expected||!expected.Equals(digest,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("The speech component manifest does not identify ASTRA's pinned OpenAI Whisper "+selected+" checkpoint.");
        string model=Path.Combine(DirectoryPath,name);
        if(!File.Exists(model))throw MissingComponent(name);
        SafeFileOperations.NoLinks(model);
        using var stream=File.OpenRead(model);
        string actual=Convert.ToHexString(await SHA256.HashDataAsync(stream,ct));
        if(!actual.Equals(expected,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("The OpenAI Whisper "+selected+" model checksum does not match its pinned manifest. No speech model was loaded.");
        return model;
    }
    internal static async Task VerifyDirectoryAsync(string directory,CancellationToken ct,bool includeModels=true)
    {
        // Compatibility call verifies only pinned speech assets, never legacy text weights/native CPU DLLs.
        if(!Path.GetFullPath(directory).Equals(Path.GetFullPath(DirectoryPath),StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Only the bundled, pinned speech component directory is supported.");
        if(includeModels)await VerifyWhisperAsync(ct);
    }

    private static FileNotFoundException MissingComponent(string name)=>new(
        "Local speech input is unavailable: missing bundled component "+name+". Re-extract the verified ASTRA local GPU release.");
}
