namespace AstraOS.Core;

public static class AstraPaths
{
    /// <summary>Optional explicit local QA profile; ordinary launches retain the original data location.</summary>
    public static string UserDataRoot
    {
        get
        {
            var directory = Environment.GetEnvironmentVariable("ASTRA_DATA_DIRECTORY");
            return !string.IsNullOrWhiteSpace(directory) && Path.IsPathFullyQualified(directory)
                ? Path.GetFullPath(directory)
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ASTRA", "OS");
        }
    }
}
