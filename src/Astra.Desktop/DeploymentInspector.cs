namespace AstraOS.Desktop;

public static class DeploymentInspector
{
    public static string Read()
    {
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var dism = Path.Combine(windows, "System32", "dism.exe");
        var roots = new[] { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) };
        var oscdimg = roots.Select(p => Path.Combine(p, "Windows Kits", "10", "Assessment and Deployment Kit", "Deployment Tools", "amd64", "Oscdimg", "oscdimg.exe")).FirstOrDefault(File.Exists);
        return $"DISM: {(File.Exists(dism) ? dism : "Non disponibile")}\nOSCDIMG / ADK: {oscdimg ?? "Non disponibile; installa Windows ADK Deployment Tools"}\n" +
            "Il piano verifica il media e i percorsi. La costruzione usa una cartella temporanea e preserva la sorgente. Avvio BIOS/UEFI da validare in una macchina virtuale prima di usare il media.";
    }
}
