namespace AstraOS.Desktop;

/// <summary>
/// Local, best-effort startup tracing for WinUI 3 diagnostic builds.
/// The log is kept on the user's machine; it is never uploaded.
/// </summary>
internal static class StartupDiagnostics
{
    internal static void Record(string stage, string? detail = null)
    {
        try
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AstraOS", "Logs");
            Directory.CreateDirectory(directory);
            var logPath = Path.Combine(directory, "startup.log");
            if (File.Exists(logPath) && new FileInfo(logPath).Length > 2_000_000)
                File.Move(logPath, Path.Combine(directory, "startup.previous.log"), overwrite: true);
            File.AppendAllText(logPath,
                $"[{DateTimeOffset.Now:O}] {stage}{Environment.NewLine}" +
                (detail is null ? string.Empty : detail + Environment.NewLine));
        }
        catch
        {
            // Diagnostic logging is best effort and must never cause a new crash.
        }
    }
}
