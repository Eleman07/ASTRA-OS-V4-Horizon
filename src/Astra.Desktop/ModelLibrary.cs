using System.Text.Json;
using AstraOS.Core;

namespace AstraOS.Desktop;
public sealed record LibraryModel(string Id,string Name,string File,string[] Tools,string Author,string License,string Source,string LicenseUrl,string Sha256);
public static class ModelLibrary
{
    public static IReadOnlyList<LibraryModel> Load()
    {
        string path=Path.Combine(AppContext.BaseDirectory,"Graphics3D","web","models","catalog.json");
        var result=JsonSerializer.Deserialize<LibraryModel[]>(System.IO.File.ReadAllText(path),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})??Array.Empty<LibraryModel>();
        if(result.Length>64||result.Any(m=>m.Id.Length>60||Path.GetFileName(m.File)!=m.File||Path.GetExtension(m.File)!=".glb"||m.Sha256.Length!=64))throw new InvalidDataException(AstraOS.Core.UiText.T("Catalogo modelli non valido."));
        return result;
    }
    public static async Task<MeshDocument> ReadAsync(LibraryModel entry,CancellationToken ct)
    {
        string path=Path.Combine(AppContext.BaseDirectory,"Graphics3D","web","models",entry.File);
        var bytes=await System.IO.File.ReadAllBytesAsync(path,ct);
        if(!Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).Equals(entry.Sha256,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException(AstraOS.Core.UiText.T("Checksum del modello non valido."));
        return MeshImport.Parse(entry.File,bytes);
    }
}
