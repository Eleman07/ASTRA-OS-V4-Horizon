using Microsoft.Win32;

namespace AstraOS.Desktop;

/// <summary>Opt-in startup for this unpackaged app, scoped to the current Windows account.</summary>
public static class LoginStartup
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "AstraOSHorizon";

    public static string CommandFor(string executable)
    {
        if (string.IsNullOrWhiteSpace(executable) || !Path.IsPathFullyQualified(executable) ||
            executable.Any(c => char.IsControl(c) || c == '"') ||
            !Path.GetFileName(executable).Equals("Astra.Desktop.exe", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(AstraOS.Core.UiText.T("Percorso dell'app ASTRA non valido."));
        string command = "\"" + Path.GetFullPath(executable) + "\"";
        if (command.Length > 260) throw new ArgumentException(AstraOS.Core.UiText.T("Percorso troppo lungo per l'avvio automatico Windows: usa una cartella più breve."));
        return command;
    }

    public static bool IsCurrentCopyRegistered()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return string.Equals(key?.GetValue(ValueName) as string, CommandFor(CurrentExecutable()), StringComparison.OrdinalIgnoreCase);
    }

    public static void SetEnabled(bool enabled)
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASTRA_DATA_DIRECTORY")))
            throw new InvalidOperationException(AstraOS.Core.UiText.T("L'avvio automatico non viene modificato dai profili QA."));
        using var key = Registry.CurrentUser.CreateSubKey(RunKey, writable: true)
            ?? throw new IOException(AstraOS.Core.UiText.T("Impossibile accedere all'avvio automatico del tuo account."));
        if (enabled)
        {
            string executable = CurrentExecutable();
            if (!File.Exists(executable)) throw new FileNotFoundException(AstraOS.Core.UiText.T("Eseguibile ASTRA non trovato."), executable);
            key.SetValue(ValueName, CommandFor(executable), RegistryValueKind.String);
        }
        else key.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    private static string CurrentExecutable() => Environment.ProcessPath
        ?? throw new InvalidOperationException(AstraOS.Core.UiText.T("Percorso del processo ASTRA non disponibile."));
}
