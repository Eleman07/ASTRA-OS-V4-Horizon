using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace AstraOS.Desktop;

/// <summary>Opt-in, read-only Windows security queries. Not a security attestation.</summary>
public static class SecurityInspector
{
    private const string Query = """
        $ErrorActionPreference='Stop'
        [Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
        $def=$null
        try { $def=Get-MpComputerStatus } catch { }
        $fire=@()
        try { $fire=@(Get-NetFirewallProfile | Select-Object Name,Enabled) } catch { }
        $realtime=$null
        $signaturesDays=$null
        if ($null -ne $def) {
          $realtime=[bool]$def.RealTimeProtectionEnabled
          if ($def.AntivirusSignatureLastUpdated) { $signaturesDays=[math]::Round(((Get-Date)-$def.AntivirusSignatureLastUpdated).TotalDays,1) }
        }
        $secureBoot=$null
        try { $secureBoot=Confirm-SecureBootUEFI -ErrorAction Stop } catch { }
        $encryption=@()
        try { $encryption=@(Get-BitLockerVolume -ErrorAction Stop | ForEach-Object { "$($_.MountPoint): $($_.ProtectionStatus)" }) } catch { }
        $memoryIntegrity=$null
        try { $guard=Get-CimInstance -Namespace root\Microsoft\Windows\DeviceGuard -ClassName Win32_DeviceGuard -ErrorAction Stop; if ($guard) { $memoryIntegrity=($guard.SecurityServicesRunning -contains 2) } } catch { }
        $lastHotFix=$null
        try { $fix=Get-CimInstance Win32_QuickFixEngineering -ErrorAction Stop | Where-Object InstalledOn | Sort-Object InstalledOn -Descending | Select-Object -First 1; if ($fix) { $lastHotFix="$($fix.HotFixID) - $($fix.InstalledOn)" } } catch { }
        [pscustomobject]@{
          memoryIntegrity=$memoryIntegrity
          lastHotFix=$lastHotFix
          secureBoot=$secureBoot
          deviceEncryption=$encryption
          defenderAvailable=($null -ne $def)
          realtime=$realtime
          signaturesDays=$signaturesDays
          firewall=@($fire | ForEach-Object { "$($_.Name): $($_.Enabled)" })
        } | ConvertTo-Json -Compress -Depth 3
        """;

    public static async Task<string> ReadAsync(CancellationToken ct = default)
    {
        if (!OperatingSystem.IsWindows()) return "Disponibile soltanto su Windows.";
        var shell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "WindowsPowerShell", "v1.0", "powershell.exe");
        var info = new ProcessStartInfo(shell)
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
        };
        // A parent PowerShell 7 session can otherwise pass incompatible module
        // directories to Windows PowerShell 5.1 and suppress ConvertTo-Json.
        info.Environment["PSModulePath"] = string.Join(Path.PathSeparator,
            Path.Combine(Path.GetDirectoryName(shell)!, "Modules"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsPowerShell", "Modules"));
        info.ArgumentList.Add("-NoProfile");
        info.ArgumentList.Add("-NonInteractive");
        info.ArgumentList.Add("-Command");
        info.ArgumentList.Add(Query);
        using var process = Process.Start(info) ?? throw new InvalidOperationException(AstraOS.Core.UiText.T("PowerShell non avviato."));
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(15));
            var outputTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var errorTask = process.StandardError.ReadToEndAsync(timeout.Token);
            await process.WaitForExitAsync(timeout.Token);
            var output = await outputTask;
            var error = await errorTask;
            if (output.Length > 32_768 || process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
                throw new InvalidDataException(AstraOS.Core.UiText.F("Controllo sicurezza non disponibile (exit {0}): ", process.ExitCode) + (string.IsNullOrWhiteSpace(error) ? AstraOS.Core.UiText.T("nessun dato restituito") : error[..Math.Min(600,error.Length)]));
            using var doc = JsonDocument.Parse(output);
            var data = doc.RootElement;
            bool defender = data.GetProperty("defenderAvailable").GetBoolean();
            string realtime = data.GetProperty("realtime").ValueKind == JsonValueKind.True ? "attiva"
                : data.GetProperty("realtime").ValueKind == JsonValueKind.False ? "non attiva" : "non disponibile";
            var fw = data.GetProperty("firewall");
            string firewall = fw.ValueKind == JsonValueKind.Array ? string.Join(", ", fw.EnumerateArray().Select(v => v.ToString()))
                : fw.ToString();
            string days = data.GetProperty("signaturesDays").ToString();
            if (string.IsNullOrWhiteSpace(firewall)) firewall = "non disponibile";
            var integrity = data.GetProperty("memoryIntegrity");
            string integrityStatus = integrity.ValueKind == JsonValueKind.True ? "in esecuzione" : integrity.ValueKind == JsonValueKind.False ? "non in esecuzione" : "non disponibile";
            string lastUpdate = data.GetProperty("lastHotFix").ToString();
            if (string.IsNullOrWhiteSpace(lastUpdate)) lastUpdate = "non disponibile";
            var boot = data.GetProperty("secureBoot");
            string bootStatus = boot.ValueKind == JsonValueKind.True ? "attivo"
                : boot.ValueKind == JsonValueKind.False ? "non attivo" : "non disponibile / verifica manuale";
            var encrypted = data.GetProperty("deviceEncryption");
            string volumes = encrypted.ValueKind == JsonValueKind.Array
                ? string.Join(", ", encrypted.EnumerateArray().Select(v => v.ToString()))
                : "verifica manuale necessaria";
            if (string.IsNullOrWhiteSpace(volumes)) volumes = "non disponibile / verifica manuale";
            return $"Microsoft Defender rilevato: {(defender ? "sì" : "non disponibile / antivirus alternativo")}\n" +
                   $"Protezione in tempo reale: {realtime}\n" +
                   $"Età firme antivirus (giorni): {(days == "" ? "sconosciuta" : days)}\n" +
                   $"Firewall Windows: {firewall}\n" +
                   $"Secure Boot UEFI: {bootStatus}\n" +
                   $"Crittografia dispositivi (dischi rilevati): {volumes}\n\n" +
                   $"Integrità memoria (HVCI): {integrityStatus}\n" +
                   $"Ultimo hotfix rilevato: {lastUpdate}\nDisponibilità nuovi aggiornamenti: verifica in Windows Update\n\n" +
                   "Rilevazione parziale senza privilegi elevati. 'Non disponibile' non equivale a vulnerabile. " +
                   "Controlla lo stato reale nelle impostazioni Windows e mantieni Windows Update attivo.";
        }
        finally
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            catch (System.ComponentModel.Win32Exception) { }
            catch (InvalidOperationException) { }
        }
    }
}
