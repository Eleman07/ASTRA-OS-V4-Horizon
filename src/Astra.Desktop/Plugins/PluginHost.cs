using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using AstraOS.Core;

namespace AstraOS.Desktop.Plugins;

/// <summary>Hash-pinned opt-in plugins. Out of process but NOT a Windows security sandbox.</summary>
public sealed class PluginHost
{
    public string PluginRoot { get; } = Path.Combine(AstraPaths.UserDataRoot, "plugins");

    public IReadOnlyList<(PluginManifest Manifest, string Folder)> Discover()
    {
        Directory.CreateDirectory(PluginRoot);
        var result = new List<(PluginManifest, string)>();
        foreach (var folder in Directory.EnumerateDirectories(PluginRoot).Take(100))
        {
            try
            {
                if (IsReparse(folder)) continue;
                var manifestPath = Path.Combine(folder, "plugin.json");
                if (!File.Exists(manifestPath) || IsReparse(manifestPath) || new FileInfo(manifestPath).Length > 16_384) continue;
                var manifest = PluginProtocol.Validate(File.ReadAllText(manifestPath), Path.GetFileName(folder));
                var executable = Path.Combine(folder, manifest.EntryPoint);
                if (!IsExpectedBinary(executable, manifest.Sha256)) continue;
                result.Add((manifest, folder));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException or ArgumentException) { }
        }
        return result;
    }
    /// <summary>Discover *data-only* extensions; safe default for public use.</summary>
    public IReadOnlyList<(DeclarativePlugin Plugin, string Folder)> DiscoverDeclarative()
    {
        var result = new List<(DeclarativePlugin, string)>();
        var roots = new[] { Path.Combine(AppContext.BaseDirectory, "recipes"), PluginRoot };
        foreach (var root in roots)
        {
            if (!Directory.Exists(root) || IsReparse(root)) continue;
            foreach (var folder in Directory.EnumerateDirectories(root).Take(100))
            {
                try
                {
                    if (IsReparse(folder)) continue;
                    var path = Path.Combine(folder, "recipe.json");
                    if (!File.Exists(path) || IsReparse(path) || new FileInfo(path).Length > 8192) continue;
                    var plugin = DeclarativePlugins.Validate(File.ReadAllText(path), Path.GetFileName(folder));
                    result.Add((plugin, folder));
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException or ArgumentException) { }
            }
        }
        return result;
    }

    public string ExecuteDeclarative(DeclarativePlugin approved, string folder, string text)
    {
        var directory = Path.GetFullPath(folder);
        var parent = Path.GetDirectoryName(directory) ?? throw new UnauthorizedAccessException(AstraOS.Core.UiText.T("Directory senza padre."));
        var permitted = new[] { Path.GetFullPath(PluginRoot), Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "recipes")) };
        if (!permitted.Any(p => string.Equals(parent, p, StringComparison.OrdinalIgnoreCase)) || IsReparse(directory))
            throw new UnauthorizedAccessException(AstraOS.Core.UiText.T("Directory non autorizzata."));
        var path = Path.Combine(directory, "recipe.json");
        if (!File.Exists(path) || IsReparse(path) || new FileInfo(path).Length > 8192)
            throw new UnauthorizedAccessException(AstraOS.Core.UiText.T("Ricetta modificata o non valida."));
        var fresh = DeclarativePlugins.Validate(File.ReadAllText(path), Path.GetFileName(directory));
        if (fresh != approved) throw new InvalidDataException(AstraOS.Core.UiText.T("Il manifesto è cambiato: ripeti la conferma."));
        return DeclarativePlugins.Transform(fresh, text);
    }

    private static bool IsReparse(string path) => (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
    private static bool IsExpectedBinary(string path, string expected)
    {
        if (!File.Exists(path) || IsReparse(path)) return false;
        // Bound hashing input. No network or download from ASTRA.
        if (new FileInfo(path).Length is < 1024 or > 250_000_000) return false;
        using var stream = File.OpenRead(path);
        return string.Equals(Convert.ToHexString(SHA256.HashData(stream)), expected, StringComparison.OrdinalIgnoreCase);
    }
    public async Task<string> ExecuteAsync(PluginManifest plugin, string folder, string text, CancellationToken ct = default)
    {
        if (text.Length > 4000) throw new ArgumentException(AstraOS.Core.UiText.T("Input troppo lungo."));
        var knownFolder = Path.GetFullPath(Path.Combine(PluginRoot, plugin.Id));
        if (!string.Equals(Path.GetFullPath(folder), knownFolder, StringComparison.OrdinalIgnoreCase) || IsReparse(knownFolder))
            throw new UnauthorizedAccessException(AstraOS.Core.UiText.T("Percorso plugin non valido."));
        var path = Path.Combine(knownFolder, "plugin.json");
        if (IsReparse(path) || new FileInfo(path).Length > 16_384) throw new UnauthorizedAccessException(AstraOS.Core.UiText.T("Manifesto non sicuro."));
        var fresh = PluginProtocol.Validate(File.ReadAllText(path), plugin.Id);
        if (fresh.ApiVersion != plugin.ApiVersion || fresh.Id != plugin.Id || fresh.Name != plugin.Name ||
            fresh.Description != plugin.Description || fresh.EntryPoint != plugin.EntryPoint ||
            !string.Equals(fresh.Sha256, plugin.Sha256, StringComparison.OrdinalIgnoreCase) ||
            !fresh.Capabilities.SequenceEqual(plugin.Capabilities))
            throw new InvalidDataException(AstraOS.Core.UiText.T("Il manifesto è cambiato: conferma nuovamente."));
        var exe = Path.Combine(knownFolder, plugin.EntryPoint);
        if (!IsExpectedBinary(exe, fresh.Sha256)) throw new InvalidDataException(AstraOS.Core.UiText.T("Hash eseguibile non corrispondente: esecuzione negata."));
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo(exe)
        {
            UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = knownFolder,
            RedirectStandardInput = true, RedirectStandardOutput = true
        };
        process.StartInfo.ArgumentList.Add("--astra-plugin-v2");
        if (!process.Start()) throw new InvalidOperationException(AstraOS.Core.UiText.T("Avvio plugin fallito."));
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(8));
            await process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(new PluginRequest(2, "transform-text", text)).AsMemory(), timeout.Token);
            process.StandardInput.Close();
            char[] buffer = new char[4096];
            var output = new System.Text.StringBuilder();
            int count;
            while ((count = await process.StandardOutput.ReadAsync(buffer.AsMemory(), timeout.Token)) > 0)
            {
                output.Append(buffer, 0, count);
                if (output.Length > 16_384) throw new InvalidDataException(AstraOS.Core.UiText.T("Output plugin troppo grande."));
            }
            await process.WaitForExitAsync(timeout.Token);
            if (process.ExitCode != 0) throw new InvalidOperationException(AstraOS.Core.UiText.T("Plugin terminato con codice diverso da zero."));
            var response = JsonSerializer.Deserialize<PluginResponse>(output.ToString()) ?? throw new InvalidDataException(AstraOS.Core.UiText.T("Risposta nulla."));
            if (response.Text is null || response.Text.Length > 12_000) throw new InvalidDataException(AstraOS.Core.UiText.T("Risposta non valida."));
            return response.Text;
        }
        finally
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
            catch (System.ComponentModel.Win32Exception) { }
        }
    }
}
