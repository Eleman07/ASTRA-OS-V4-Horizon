namespace AstraOS.Core.Agents;
// Retained settings/API name for existing user profiles. The default is Ministral, with no legacy weights.
public static class PrecisionModel
{
    public static string ModelPath=>LocalModelCatalog.Ministral.Path;
    public static Task<string> EnsureAsync(CancellationToken ct)=>ModelDownloads.EnsureAsync(LocalModelCatalog.Ministral,ct);
}
