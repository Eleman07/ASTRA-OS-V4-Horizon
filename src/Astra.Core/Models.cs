using System.Text.Json.Serialization;

namespace AstraOS.Core;

public sealed class ProfileManifest
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; init; }
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";
    [JsonPropertyName("description")]
    public string Description { get; init; } = "";
    [JsonPropertyName("packages")]
    public List<string> Packages { get; init; } = new();
}

public sealed class AstraPreferences
{
    [JsonPropertyName("selectedProfiles")]
    public List<string> SelectedProfiles { get; set; } = new() { "core" };
    // Defaults keep expensive subsystems off until explicitly requested.
    [JsonPropertyName("ecoMode")]
    public bool EcoMode { get; set; } = false;
    [JsonPropertyName("reducedMotion")]
    public bool ReducedMotion { get; set; } = false;
    [JsonPropertyName("orbitFpsCap")]
    public int OrbitFpsCap { get; set; } = 30;
    [JsonPropertyName("showDock")]
    public bool ShowDock { get; set; } = true;
    [JsonPropertyName("focusMode")]
    public bool FocusMode { get; set; } = false;
    [JsonPropertyName("themeCursorEnabled")]
    public bool ThemeCursorEnabled { get; set; } = false;
    public int CursorSize { get; set; } = 48;
    [JsonPropertyName("startFullScreen")]
    public bool StartFullScreen { get; set; } = false;
    [JsonPropertyName("palette")]
    public AstraPalette Palette { get; set; } = AstraPalette.Default;

    [JsonPropertyName("activeWorkspace")]
    public string ActiveWorkspace { get; set; } = "core";
    public string AiExecutablePath { get; set; } = "";
    public string AiModelPath { get; set; } = "";
    [JsonPropertyName("aiProvider")]
    public string AiProvider { get; set; } = "openrouter";
    [JsonPropertyName("openRouterModel")]
    public string OpenRouterModel { get; set; } = "nvidia/nemotron-3-ultra-550b-a55b:free";
    public string StudyDomain { get; set; } = "engineering";
    public string EngineeringTrack { get; set; } = "aerospace";
    public string BannerImagePath { get; set; } = "";
    public string WallpaperImagePath { get; set; } = "";
    [JsonPropertyName("favoriteApps")]
    public List<string> FavoriteApps { get; set; } = new();
    [JsonPropertyName("recentApps")]
    public List<string> RecentApps { get; set; } = new();
    [JsonPropertyName("projects")]
    public List<WorkspaceProject> Projects { get; set; } = new();
    [JsonPropertyName("disabledPlugins")]
    public List<string> DisabledPlugins { get; set; } = new();

}

public sealed record WorkspaceProject(string Name, string Directory, string Workspace);

public sealed record CuratedPackage(string Id, string Name, string Category, string Description);
