namespace AstraOS.Core;

/// <summary>
/// Only these exact package identifiers can ever be handed to WinGet.
/// Editing a JSON manifest cannot add executable commands or untrusted IDs.
/// </summary>
public static class CuratedCatalog
{
    public static readonly IReadOnlyDictionary<string, CuratedPackage> Packages =
        new Dictionary<string, CuratedPackage>(StringComparer.Ordinal)
        {
            ["Git.Git"] = new("Git.Git", "Git", "STEM", "Controllo di versione"),
            ["Microsoft.VisualStudioCode"] = new("Microsoft.VisualStudioCode", "Visual Studio Code", "STEM", "Editor e sviluppo"),
            ["Python.Python.3.13"] = new("Python.Python.3.13", "Python 3.13", "STEM", "Calcolo scientifico e scripting"),
            ["Kitware.CMake"] = new("Kitware.CMake", "CMake", "STEM", "Build system C/C++"),
            ["Valve.Steam"] = new("Valve.Steam", "Steam", "Gaming", "Libreria e distribuzione giochi"),
            ["EpicGames.EpicGamesLauncher"] = new("EpicGames.EpicGamesLauncher", "Epic Games Launcher", "Gaming", "Launcher giochi"),
            ["Discord.Discord"] = new("Discord.Discord", "Discord", "Gaming", "Comunità e comunicazione"),
            ["BlenderFoundation.Blender"] = new("BlenderFoundation.Blender", "Blender", "Creator", "Modellazione 3D e rendering"),
            ["OBSProject.OBSStudio"] = new("OBSProject.OBSStudio", "OBS Studio", "Creator", "Registrazione e streaming"),
            ["GIMP.GIMP"] = new("GIMP.GIMP", "GIMP", "Creator", "Grafica e fotoritocco"),
        };

    public static readonly string[] ProfileIds = { "core", "stem", "gaming", "creator" };

    public static CuratedPackage Get(string id) =>
        Packages.TryGetValue(id, out var package)
            ? package
            : throw new ArgumentException("Pacchetto non autorizzato", nameof(id));
}
