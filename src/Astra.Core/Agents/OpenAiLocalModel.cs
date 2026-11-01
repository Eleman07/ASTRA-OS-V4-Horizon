namespace AstraOS.Core.Agents;
public static class OpenAiLocalModel
{
    public static string ModelPath=>LocalModelCatalog.GptOss.Path;
    public static Task<string> EnsureAsync(CancellationToken ct)=>ModelDownloads.EnsureAsync(LocalModelCatalog.GptOss,ct);
}
