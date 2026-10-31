using System.Security.Cryptography;
using System.Text.Json;

namespace AstraOS.Desktop.Ai;
public static class BundledAi
{
    private static bool _verified;
    private static readonly SemaphoreSlim Gate=new(1,1);
    public static string DirectoryPath=>Path.Combine(AppContext.BaseDirectory,"Ai","components");
    public static string ModelPath=>Path.Combine(DirectoryPath,"astra-local-model.gguf");
    public static string RuntimePath=>Path.Combine(AppContext.BaseDirectory,"LLamaSharp.dll");
    public static bool IsLegacyBundledRuntime(string path)
    {
        if(string.IsNullOrWhiteSpace(path))return false;
        string full=Path.GetFullPath(path.Trim().Trim('"'));
        // A previous portable folder may have moved. Only recognize the old
        // bundled layout; user-selected external runtimes keep their setting.
        return full.EndsWith(Path.Combine("Ai","components","llama-server.exe"),StringComparison.OrdinalIgnoreCase);
    }
    public static async Task VerifyAsync(CancellationToken ct)
    {
        await Gate.WaitAsync(ct);
        try
        {
            if(_verified&&File.Exists(RuntimePath)&&File.Exists(ModelPath))return;
            _verified=false;
            await VerifyDirectoryAsync(DirectoryPath,ct);
            await VerifyRuntimeAsync(ct);
            _verified=true;
        }
        finally{Gate.Release();}
    }

    public static async Task VerifyRuntimeAsync(CancellationToken ct)
    {
        string manifest=Path.Combine(AppContext.BaseDirectory,"Ai","embedded-runtime.json");
        if(!File.Exists(manifest))throw MissingComponent("embedded-runtime.json");
        using var document=JsonDocument.Parse(await File.ReadAllTextAsync(manifest,ct));
        string root=Path.GetFullPath(AppContext.BaseDirectory);
        foreach(var file in document.RootElement.GetProperty("files").EnumerateObject())
        {
            ct.ThrowIfCancellationRequested();string path=Path.GetFullPath(Path.Combine(root,file.Name));
            if(Path.IsPathRooted(file.Name)||!path.StartsWith(root,StringComparison.OrdinalIgnoreCase)||file.Name.Split('/','\\').Any(p=>p is "." or ".."))throw new InvalidDataException(AstraOS.Core.UiText.T("Manifest AI non valido."));
            AstraOS.Core.SafeFileOperations.NoLinks(path);if(!File.Exists(path))throw MissingComponent(file.Name);
            using var stream=File.OpenRead(path);string hash=Convert.ToHexString(await SHA256.HashDataAsync(stream,ct));
            if(!hash.Equals(file.Value.GetString(),StringComparison.OrdinalIgnoreCase))throw new InvalidDataException(AstraOS.Core.UiText.T("Componente AI modificato o incompleto: ")+file.Name);
        }
    }
    internal static async Task VerifyDirectoryAsync(string directory,CancellationToken ct,bool includeModels=true)
    {
        string manifest=Path.Combine(directory,"manifest.json");
        if(!File.Exists(manifest))throw MissingComponent("manifest.json");
        using var d=JsonDocument.Parse(await File.ReadAllTextAsync(manifest,ct));
        foreach(var property in d.RootElement.GetProperty("files").EnumerateObject())
        {
            if(!includeModels&&(property.Name.EndsWith(".gguf",StringComparison.OrdinalIgnoreCase)||property.Name.EndsWith(".bin",StringComparison.OrdinalIgnoreCase)))continue;
            ct.ThrowIfCancellationRequested();
            if(Path.GetFileName(property.Name)!=property.Name)throw new InvalidDataException(AstraOS.Core.UiText.T("Manifest AI non valido."));
            string path=Path.Combine(directory,property.Name);
            if(!File.Exists(path))throw MissingComponent(property.Name);
            using var stream=File.OpenRead(path);var hash=await SHA256.HashDataAsync(stream,ct);
            if(!Convert.ToHexString(hash).Equals(property.Value.GetString(),StringComparison.OrdinalIgnoreCase))throw new InvalidDataException(AstraOS.Core.UiText.T("Componente AI modificato o incompleto: ")+property.Name);
        }
    }

    private static FileNotFoundException MissingComponent(string name)=>new(
        AstraOS.Core.UiText.T("AI locale non disponibile: manca un componente incluso. Controlla l'estrazione del pacchetto e la cronologia dell'antivirus prima di riprovare.")+" ("+name+")");
}
