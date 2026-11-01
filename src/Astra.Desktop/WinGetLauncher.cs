using System.Diagnostics;
using AstraOS.Core;

namespace AstraOS.Desktop;

public static class WinGetLauncher
{
    public static async Task<int> InstallAsync(string packageId, CancellationToken cancellationToken)
    {
        var winget = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "WindowsApps", "winget.exe");
        if (!File.Exists(winget)) throw new FileNotFoundException(AstraOS.Core.UiText.T("WinGet non disponibile. Installa App Installer dal Microsoft Store."));
        var info = new ProcessStartInfo(winget) { UseShellExecute = true,
            WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) };
        foreach (var argument in CommandSafety.WingetArguments(packageId)) info.ArgumentList.Add(argument);
        using var process = Process.Start(info) ?? throw new InvalidOperationException(AstraOS.Core.UiText.T("WinGet non avviato."));
        // Cancellation releases ASTRA's wait; it does not terminate a running installer.
        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode;
    }
    public static bool TryLaunch(string packageId, out string problem)
    {
        problem = "";
        var args = CommandSafety.WingetArguments(packageId);
        // Absolute per-user path avoids PATH/current-directory hijacking.
        var winget = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "WindowsApps", "winget.exe");
        if (!File.Exists(winget))
        {
            problem = "WinGet non è stato trovato. Installa/aggiorna App Installer dal Microsoft Store oppure copia il comando.";
            return false;
        }
        try
        {
            var info = new ProcessStartInfo
            {
                FileName = winget,
                UseShellExecute = true,
                WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            };
            foreach (var arg in args) info.ArgumentList.Add(arg);
            // Interactive installation: no silent mode; EULAs are never accepted by ASTRA.
            using var process = Process.Start(info);
            if (process is null)
            {
                problem = "WinGet non è stato avviato.";
                return false;
            }
            return true; // Launch only; NEVER claim the package was installed.
        }
        catch (Exception error) when (error is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            problem = "Impossibile avviare WinGet: " + error.Message;
            return false;
        }
    }
}
