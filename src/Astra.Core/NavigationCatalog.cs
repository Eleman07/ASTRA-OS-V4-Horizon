using System.Globalization;
using System.Text;

namespace AstraOS.Core;

/// <summary>Deterministic, bounded, no-execution navigation; zero filesystem or network access.</summary>
public static class NavigationCatalog
{
    private static readonly (string Page, string Label, string[] Aliases)[] Destinations =
    {
        ("home", "Panoramica", new[] { "home", "core", "inizio", "dashboard" }),
        ("files", "File Explorer", new[] { "files", "file", "cartelle", "esplora", "documenti", "download" }),
        ("multitasking", "Multitasking e sessioni", new[] { "multitasking", "finestre", "sessioni", "snap", "session" }),
        ("widgets", "Widget", new[] { "widget", "widgets", "automazioni", "automation" }),
        ("assistant", "ASTRA Assistant", new[] { "assistente", "assistant", "astra assistant", "copilot", "voce", "ocr" }),
        ("system", "Centro di controllo", new[] { "sistema", "system", "centro controllo", "control center", "macos", "windows 11", "quick look", "appunti" }),
        ("windows-tools", "Strumenti Windows", new[] { "strumenti windows", "utilità", "accessibilità", "accessibility", "tools" }),
        ("profiles", "Profili e combinazioni", new[] { "profili", "workspaces" }),
        ("stem", "ASTRA STEM", new[] { "stem", "studio", "engineering", "mission", "dataset", "python", "jupyter", "matlab" }),
        ("gaming", "ASTRA Gaming", new[] { "gaming", "giochi" }),
        ("creator", "ASTRA Creator", new[] { "creator", "creatività", "rendering", "progetti" }),
        ("theme", "Personalizza ASTRA", new[] { "tema", "palette", "impostazioni", "settings" }),
        ("catalog", "App Hub", new[] { "app", "software", "catalogo", "installazioni", "blender", "visual studio", "steam", "obs", "gimp" }),
        ("diagnostics", "Performance Center", new[] { "prestazioni", "performance", "ram", "cpu", "diagnostica" }),
        ("security", "Security Center", new[] { "sicurezza", "security", "defender", "firewall", "privacy" }),
        ("ubuntu", "Ubuntu Lab", new[] { "ubuntu", "linux", "wsl", "bash", "apt" }),
        ("brain", "Second Brain", new[] { "brain", "ai", "intelligenza", "appunti", "note", "flashcard" }),
        ("plugins", "Plugin SDK", new[] { "plugin", "plugins", "estensioni", "sdk" }),
        ("orbit", "Orbital Studio 3D", new[] { "3d", "orbital", "simulazione", "webgl" }),
        ("iso", "ISO Builder", new[] { "iso", "builder", "immagine", "windows adk" })
    };

    public static IReadOnlyList<string> Routes => Destinations.Select(d=>d.Page).Append("core").ToArray();

    public static IReadOnlyList<(string Page, string Label)> Suggest(string? query, int limit = 5)
    {
        var text = Normalize(query);
        if (text.Length == 0) return Array.Empty<(string, string)>();
        if (text.Length > 96) return Array.Empty<(string, string)>();
        return Destinations.Select(d => (d.Page, d.Label, Rank: Rank(d, text)))
            .Where(x => x.Rank > 0).OrderByDescending(x => x.Rank)
            .ThenBy(x => x.Label, StringComparer.Ordinal).Take(Math.Clamp(limit, 1, 10))
            .Select(x => (x.Page, x.Label)).ToArray();
    }
    public static string? Resolve(string? query) => Suggest(query, 1).FirstOrDefault().Page;

    private static int Rank((string Page, string Label, string[] Aliases) d, string query)
    {
        var names = d.Aliases.Append(d.Page).Append(d.Label);
        if (names.Any(x => Normalize(x) == query)) return 100;
        if (names.Any(x => Normalize(x).StartsWith(query, StringComparison.Ordinal))) return 70;
        if (names.Any(x => Normalize(x).Contains(query, StringComparison.Ordinal))) return 30;
        // No fuzzy typo suggestions that might navigate somewhere unintended.
        return 0;
    }
    private static string Normalize(string? input)
    {
        var s = (input ?? "").Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(Math.Min(96, s.Length));
        foreach (var ch in s)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark &&
                !char.IsControl(ch)) builder.Append(ch);
        }
        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
