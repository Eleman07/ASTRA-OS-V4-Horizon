using System.Security.Cryptography;
using System.Text.Json;
namespace AstraOS.Desktop.Ai;
internal static class GpuRuntime
{
    public static string DirectoryPath=>Path.Combine(AppContext.BaseDirectory,"Ai","gpu");
    public static string Executable=>Path.Combine(DirectoryPath,"llama-server.exe");
    public static bool Available=>File.Exists(Executable)&&File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"vulkan-1.dll"));
    private static bool _verified;
    public static async Task VerifyAsync(CancellationToken ct)
    {
        if(_verified)return;
        using var manifest=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(DirectoryPath,"manifest.json"),ct));
        foreach(var file in manifest.RootElement.GetProperty("files").EnumerateObject())
        {
            if(Path.GetFileName(file.Name)!=file.Name)throw new InvalidDataException("Invalid GPU runtime manifest.");
            string path=Path.Combine(DirectoryPath,file.Name);AstraOS.Core.SafeFileOperations.NoLinks(path);
            using var stream=File.OpenRead(path);string hash=Convert.ToHexString(await SHA256.HashDataAsync(stream,ct));
            if(!hash.Equals(file.Value.GetString(),StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("GPU runtime checksum mismatch.");
        }
        _verified=true;
    }
}
