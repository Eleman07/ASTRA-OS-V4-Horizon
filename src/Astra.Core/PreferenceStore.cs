using System.Text.Json;

namespace AstraOS.Core;

public sealed class PreferenceStore
{
    public string DirectoryPath { get; }
    public string FilePath => Path.Combine(DirectoryPath, "preferences.json");

    public PreferenceStore(string? directory = null)
    {
        DirectoryPath = directory ?? AstraPaths.UserDataRoot;
    }

    public AstraPreferences Load()
    {
        if (!File.Exists(FilePath)) return new AstraPreferences();
        try
        {
            var settings = JsonSerializer.Deserialize<AstraPreferences>(File.ReadAllText(FilePath))
                ?? new AstraPreferences();
            var sanitized = (settings.SelectedProfiles ?? new List<string>())
                .Where(id => CuratedCatalog.ProfileIds.Contains(id, StringComparer.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            sanitized.Remove("core");
            sanitized.Insert(0, "core");
            settings.SelectedProfiles = sanitized;
            settings.OrbitFpsCap = PerformancePolicy.NormalizeFps(settings.OrbitFpsCap);
            settings.Palette = AstraPalette.Sanitize(settings.Palette);
        if(settings.CursorSize is not(32 or 48 or 64))settings.CursorSize=48;
            SanitizeWorkspace(settings);
            return settings;
        }
        catch (JsonException) { return new AstraPreferences(); }
        catch (IOException) { return new AstraPreferences(); }
        catch (UnauthorizedAccessException) { return new AstraPreferences(); }
    }

    internal static List<AstraTaskItem> SanitizeTasks(List<AstraTaskItem>? tasks)
    {
        return (tasks ?? new()).Where(t => t is not null && !string.IsNullOrWhiteSpace(t.Text) && t.Text.Length <= 120
            && Guid.TryParseExact(t.Id, "N", out _))
            .GroupBy(t => t.Id, StringComparer.Ordinal).Select(g => g.First()).Take(100).ToList();
    }

    public void Save(AstraPreferences settings)
    {
        Directory.CreateDirectory(DirectoryPath);
        var filtered = (settings.SelectedProfiles ?? new List<string>())
            .Where(id => CuratedCatalog.ProfileIds.Contains(id, StringComparer.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        filtered.Remove("core");
        filtered.Insert(0, "core");
        settings.SelectedProfiles = filtered;
        settings.OrbitFpsCap = PerformancePolicy.NormalizeFps(settings.OrbitFpsCap);
        settings.Palette = AstraPalette.Sanitize(settings.Palette);
        if(settings.CursorSize is not(32 or 48 or 64))settings.CursorSize=48;
        SanitizeWorkspace(settings);
        var temp = Path.Combine(DirectoryPath, ".prefs-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(settings,
                new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, FilePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }

    private static void SanitizeWorkspace(AstraPreferences settings)
    {
        settings.AiExecutablePath = SanitizeLocalPath(settings.AiExecutablePath);
        settings.AiModelPath = SanitizeLocalPath(settings.AiModelPath);
        settings.AiProvider = "openrouter";
        settings.OpenRouterModel = settings.OpenRouterModel!="openrouter/free" && IsSafeOpenRouterModel(settings.OpenRouterModel) ? settings.OpenRouterModel : "nvidia/nemotron-3-ultra-550b-a55b:free";
        settings.BannerImagePath = SanitizeLocalPath(settings.BannerImagePath);
        settings.WallpaperImagePath = SanitizeLocalPath(settings.WallpaperImagePath);
        if(!StudyCatalog.Domains.Any(x=>x.Id==settings.StudyDomain))settings.StudyDomain="engineering";
        if(!StudyCatalog.Engineering.Any(x=>x.Id==settings.EngineeringTrack))settings.EngineeringTrack="aerospace";
        if (!CuratedCatalog.ProfileIds.Contains(settings.ActiveWorkspace)) settings.ActiveWorkspace = "core";
        settings.FavoriteApps = (settings.FavoriteApps ?? new()).Where(IsAppId).Distinct().Take(100).ToList();
        settings.RecentApps = (settings.RecentApps ?? new()).Where(IsAppId).Distinct().Take(12).ToList();
        settings.DisabledPlugins = (settings.DisabledPlugins ?? new()).Where(x => !string.IsNullOrWhiteSpace(x) && x.Length <= 160).Distinct().Take(100).ToList();
        settings.Projects = (settings.Projects ?? new()).Where(p => p is not null &&
            !string.IsNullOrWhiteSpace(p.Name) && p.Name.Length <= 120 && p.Directory is not null &&
            p.Directory.Length <= 2048 && Path.IsPathFullyQualified(p.Directory) && CuratedCatalog.ProfileIds.Contains(p.Workspace))
            .DistinctBy(p => p.Directory, StringComparer.OrdinalIgnoreCase).Take(100).ToList();
    }
    private static bool IsSafeOpenRouterModel(string? model) => !string.IsNullOrWhiteSpace(model) && model.Length <= 200 &&
        model.All(c => char.IsAsciiLetterOrDigit(c) || c is '/' or '-' or '_' or '.' or ':') &&
        (model == "nvidia/nemotron-3-ultra-550b-a55b:free" || model.EndsWith(":free", StringComparison.Ordinal));
    private static bool IsAppId(string? value) => value is not null && value.Length == 64 && value.All(Uri.IsHexDigit);
    private static string SanitizeLocalPath(string? path) => !string.IsNullOrWhiteSpace(path) && path.Length <= 2048 && !path.Any(char.IsControl) && Path.IsPathFullyQualified(path) ? path : "";
}
