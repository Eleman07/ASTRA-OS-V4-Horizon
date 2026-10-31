using System.Security.Cryptography;
using System.Text;

namespace AstraOS.Desktop;

public sealed record InstalledApplication(string Id, string Name, string Shortcut, string Workspace);

public static class InstalledAppCatalog
{
    public static IReadOnlyList<InstalledApplication> Discover(CancellationToken cancellationToken)
    {
        var results = new List<InstalledApplication>();
        foreach (var root in new[] { Environment.SpecialFolder.Programs, Environment.SpecialFolder.CommonPrograms })
        {
            var folder = Environment.GetFolderPath(root);
            if (!Directory.Exists(folder)) continue;
            foreach (var file in Directory.EnumerateFiles(folder, "*.lnk", new EnumerationOptions
            { RecurseSubdirectories = true, IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint }))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var name = Path.GetFileNameWithoutExtension(file);
                if (name.Contains("uninstall", StringComparison.OrdinalIgnoreCase) || name.Contains("disinstalla", StringComparison.OrdinalIgnoreCase)) continue;
                var text = name.ToLowerInvariant();
                var workspace = new[] { "steam", "epic", "game", "xbox", "discord" }.Any(text.Contains) ? "gaming" :
                    new[] { "blender", "obs", "gimp", "adobe", "photoshop", "davinci", "inkscape", "premiere" }.Any(text.Contains) ? "creator" :
                    new[] { "python", "jupyter", "matlab", "visual studio", "code", "git", "cad", "ansys", "flight", "octave" }.Any(text.Contains) ? "stem" : "core";
                results.Add(new(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(file.ToUpperInvariant()))), name, file, workspace));
                if (results.Count >= 2000) return results;
            }
        }
        return results.DistinctBy(a => a.Name, StringComparer.OrdinalIgnoreCase).OrderBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase).ToArray();
    }
}
