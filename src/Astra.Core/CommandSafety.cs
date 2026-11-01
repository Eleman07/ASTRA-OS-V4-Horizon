namespace AstraOS.Core;

public static class CommandSafety
{
    public static IReadOnlyList<string> WingetArguments(string packageId)
    {
        // Only allow IDs compiled into the application: no remote JSON execution.
        _ = CuratedCatalog.Get(packageId);
        return new[] { "install", "--id", packageId, "--exact", "--source", "winget" };
    }

    public static string DisplayWingetCommand(string packageId)
    {
        return "winget " + string.Join(' ', WingetArguments(packageId));
    }
}
