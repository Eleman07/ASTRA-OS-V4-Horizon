using System.Text.Json;

namespace AstraOS.Core;

public sealed class ProfileCatalog
{
    public IReadOnlyDictionary<string, ProfileManifest> Profiles { get; }

    private ProfileCatalog(Dictionary<string, ProfileManifest> profiles) => Profiles = profiles;

    public static ProfileCatalog Load(string directory)
    {
        var profiles = new Dictionary<string, ProfileManifest>(StringComparer.Ordinal);
        foreach (var profileId in CuratedCatalog.ProfileIds)
        {
            // Profile filenames are compiled constants: no path traversal via manifests.
            var path = Path.Combine(directory, $"{profileId}.json");
            var manifest = JsonSerializer.Deserialize<ProfileManifest>(File.ReadAllText(path))
                ?? throw new InvalidDataException($"Profilo vuoto: {profileId}");
            if (manifest.SchemaVersion != 1 || manifest.Id != profileId ||
                string.IsNullOrWhiteSpace(manifest.Name) || manifest.Name.Length > 72 ||
                manifest.Description is null || manifest.Description.Length > 240 || manifest.Packages is null ||
                manifest.Packages.Count > 20)
                throw new InvalidDataException($"Profilo non valido: {profileId}");
            if (manifest.Packages.Distinct(StringComparer.Ordinal).Count() != manifest.Packages.Count)
                throw new InvalidDataException($"Pacchetti duplicati: {profileId}");
            if (manifest.Packages.Any(p => string.IsNullOrWhiteSpace(p) || !CuratedCatalog.Packages.ContainsKey(p)))
                throw new InvalidDataException($"Pacchetto non autorizzato: {profileId}");
            if (profileId == "core" && manifest.Packages.Count != 0)
                throw new InvalidDataException("Core non può installare pacchetti automaticamente.");
            profiles.Add(profileId, manifest);
        }
        return new ProfileCatalog(profiles);
    }

    public IReadOnlyList<CuratedPackage> PackagesFor(IEnumerable<string> selectedProfiles)
    {
        var unique = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<CuratedPackage>();
        foreach (var profileId in selectedProfiles)
        {
            if (!Profiles.TryGetValue(profileId, out var profile)) continue;
            foreach (var packageId in profile.Packages)
                if (unique.Add(packageId)) result.Add(CuratedCatalog.Get(packageId));
        }
        return result;
    }
}
