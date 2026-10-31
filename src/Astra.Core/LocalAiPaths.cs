namespace AstraOS.Core;

public sealed record LocalAiSelection(string Runtime,string Model,bool Embedded);

/// <summary>Relocate only known bundled resources when a portable copy moves.</summary>
public static class LocalAiPaths
{
    private static string Clean(string? value)=>value?.Trim().Trim('"')??"";
    public static bool IsBundledRuntime(string? value)
    {
        string path=Clean(value);
        return path.Length==0||Path.GetFileName(path).Equals("LLamaSharp.dll",StringComparison.OrdinalIgnoreCase)
            ||path.EndsWith(Path.Combine("Ai","components","llama-server.exe"),StringComparison.OrdinalIgnoreCase);
    }
    public static bool IsBundledModel(string? value)
    {
        string path=Clean(value);
        return path.Length==0||path.EndsWith(Path.Combine("Ai","components","astra-local-model.gguf"),StringComparison.OrdinalIgnoreCase);
    }
    public static LocalAiSelection Resolve(string applicationDirectory,string? runtime,string? model)
    {
        string root=Path.GetFullPath(applicationDirectory);
        bool embedded=IsBundledRuntime(runtime);
        string engine=embedded?Path.Combine(root,"LLamaSharp.dll"):Path.GetFullPath(Clean(runtime));
        string weights=IsBundledModel(model)?Path.Combine(root,"Ai","components","astra-local-model.gguf"):Path.GetFullPath(Clean(model));
        return new(engine,weights,embedded);
    }
}
