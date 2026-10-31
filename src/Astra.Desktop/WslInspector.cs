using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace AstraOS.Desktop;

/// <summary>Read-only WSL inspection and opt-in Microsoft-signed Windows tool launch.</summary>
internal static class WslInspector
{
    private static string WslExe => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "wsl.exe");

    public static async Task<string> GetStatusAsync(CancellationToken ct = default)
    {
        if (!OperatingSystem.IsWindows()) return "Controllo disponibile soltanto su Windows 11.";
        if (!File.Exists(WslExe)) return "wsl.exe non trovato. Verifica i componenti Windows opzionali.";
        var (listCode, quiet) = await ExecuteReadOnlyAsync(new[] { "--list", "--quiet" }, ct);
        var names = listCode == 0 ? ParseDistributionNames(quiet) : Array.Empty<string>();
        if (names.Count == 0) return "Nessuna distribuzione WSL utilizzabile rilevata. Installa Ubuntu con il pulsante dedicato, completa la creazione dell'utente Linux nella finestra esterna e ripeti la verifica.\n\n" + quiet;
        // Fixed arguments, no shell and no user-supplied command strings.
        var (code, output) = await ExecuteReadOnlyAsync(new[] { "--list", "--verbose" }, ct);
        if (code != 0) return "WSL non pronto o nessuna distribuzione installata. " + output;
        return "Distribuzioni WSL rilevate:\n" + output +
            "\nWSL 2 è opzionale e non fornisce protezioni del kernel Ubuntu alle applicazioni Windows.";
    }

    private static async Task<(int Code, string Output)> ExecuteReadOnlyAsync(string[] args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(WslExe) {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true
        };
        foreach (var arg in args) psi.ArgumentList.Add(arg);
        using var process = Process.Start(psi) ?? throw new InvalidOperationException(AstraOS.Core.UiText.T("WSL non avviabile."));
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            var stdout = ReadOutputAsync(process.StandardOutput.BaseStream, timeout.Token);
            var stderr = ReadOutputAsync(process.StandardError.BaseStream, timeout.Token);
            await process.WaitForExitAsync(timeout.Token);
            string output = (await stdout) + " " + (await stderr);
            if (output.Length > 4096) output = output[..4096];
            return (process.ExitCode, output.Trim());
        }
        finally
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
            catch (Win32Exception) { }
        }
    }

    private static async Task<string> ReadOutputAsync(Stream stream, CancellationToken ct)
    {
        var bytes = new byte[32768]; int used = 0;
        while (used < bytes.Length)
        {
            int count = await stream.ReadAsync(bytes.AsMemory(used), ct);
            if (count == 0) break;
            used += count;
        }
        if (used == bytes.Length) throw new InvalidDataException(AstraOS.Core.UiText.T("Output WSL superiore al limite."));
        bool unicode = used >= 2 && (bytes[0] == 0xFF && bytes[1] == 0xFE ||
            Enumerable.Range(0, Math.Min(used / 2, 64)).Count(i => bytes[2*i+1] == 0) > Math.Min(used / 2, 64) / 2);
        return (unicode ? Encoding.Unicode : Encoding.UTF8).GetString(bytes, 0, used).Trim('\uFEFF', '\0', ' ', '\r', '\n');
    }

    public static bool TryStartUbuntuInstall(out string message) => Launch(new[] { "--install", "-d", "Ubuntu" }, true, out message);
    public static IReadOnlyList<string> ParseDistributionNames(string output) => output.Replace("\0", "").Trim('\uFEFF')
        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim())
        .Where(s => System.Text.RegularExpressions.Regex.IsMatch(s, @"\A[A-Za-z0-9][A-Za-z0-9_.-]{0,79}\z"))
        .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    public static string? SelectUbuntu(IEnumerable<string> names) => names
        .Where(s => s.Equals("Ubuntu", StringComparison.OrdinalIgnoreCase) || s.StartsWith("Ubuntu-", StringComparison.OrdinalIgnoreCase))
        .OrderBy(s => s.Equals("Ubuntu", StringComparison.OrdinalIgnoreCase) ? 0 : 1).ThenByDescending(s => s, StringComparer.OrdinalIgnoreCase).FirstOrDefault();

    public static async Task<string> OpenUbuntuAsync(CancellationToken ct = default)
    {
        if (!OperatingSystem.IsWindows() || !File.Exists(WslExe)) return "WSL non disponibile. Installa WSL e una distribuzione Ubuntu prima di aprire il terminale.";
        var (code, output) = await ExecuteReadOnlyAsync(new[] { "--list", "--quiet" }, ct);
        var names = code == 0 ? ParseDistributionNames(output) : Array.Empty<string>();
        var ubuntu = SelectUbuntu(names);
        if (ubuntu is null) return "Ubuntu non risulta installato. Nessun terminale avviato. Usa Installa Ubuntu, completa la prima configurazione e premi Verifica WSL.\n" +
            (names.Count > 0 ? "Distribuzioni presenti: " + string.Join(", ", names) : output);
        // Verify the registered distro can actually start before opening an interactive terminal.
        var (ready, problem) = await ExecuteReadOnlyAsync(new[] { "--distribution", ubuntu, "--exec", "/bin/true" }, ct);
        if (ready != 0) return "La distribuzione " + ubuntu + " è registrata ma non è pronta. Nessun terminale aperto.\n" + problem;
        return Launch(new[] { "--distribution", ubuntu }, false, out var message)
            ? "Terminale richiesto per " + ubuntu + ". La distribuzione è stata rilevata e il controllo di avvio è riuscito."
            : message;
    }

    private static bool Launch(string[] arguments, bool admin, out string message)
    {
        message = "";
        if (!OperatingSystem.IsWindows() || !File.Exists(WslExe))
        { message = AstraOS.Core.UiText.T("WSL non disponibile in questa installazione di Windows."); return false; }
        try
        {
            var psi = new ProcessStartInfo(WslExe) {
                UseShellExecute = true,
                WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            };
            if (admin) psi.Verb = "runas";
            foreach (var arg in arguments) psi.ArgumentList.Add(arg);
            Process.Start(psi);
            return true; // Means launched, NOT installed / success.
        }
        catch (Win32Exception ex) { message = AstraOS.Core.UiText.T("Avvio negato o annullato: ") + ex.Message; return false; }
        catch (InvalidOperationException ex) { message = AstraOS.Core.UiText.T("Impossibile avviare WSL: ") + ex.Message; return false; }
    }
}

