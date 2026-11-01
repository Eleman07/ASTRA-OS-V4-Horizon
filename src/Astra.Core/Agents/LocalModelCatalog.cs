namespace AstraOS.Core.Agents;

public sealed record LocalModelSpec(string Id,string Label,string FileName,long Bytes,string Sha256,string Repository,string Revision,string License)
{
    public string Url=>$"https://huggingface.co/{Repository}/resolve/{Revision}/{FileName}";
    public string Path=>System.IO.Path.Combine(AstraPaths.UserDataRoot,"ai","models",FileName);
    public string Publisher => Id switch { "gpt-oss" => "OpenAI · United States", "phi4" => "Microsoft · United States", "nomic" => "Nomic · United States", "ministral" or "ministral-reasoning-14" or "ministral-reasoning-8" => "Mistral AI · France", _ => "Unverified publisher" };
    public uint NativeContext => Id switch { "gpt-oss" => 131072, "phi4" => 16384, "nomic" => 2048, "ministral" or "ministral-reasoning-14" or "ministral-reasoning-8" => 262144, _ => 0 };
}

/// <summary>Reviewed, revision-pinned open weights. No hosted inference or paid API is involved.</summary>
public static class LocalModelCatalog
{
    public static readonly LocalModelSpec Ministral=new("ministral","Mistral Ministral 3 8B","Ministral-3-8B-Instruct-2512-Q4_K_M.gguf",5198911904,"33e7a72cf5e6e2cfc2f2847075acc013d68bba023e35310cef86b5cf8fdca761","mistralai/Ministral-3-8B-Instruct-2512-GGUF","0102285ad796bd99af90f58de616092e5630e970","Apache-2.0");
    public static readonly LocalModelSpec Phi=new("phi4","Microsoft Phi-4 14B","phi-4-Q4_K.gguf",9053114560,"5652b9be0ea4ae2842130d04fe31bc869fcb99a2b7106c53b4e754a343fd688f","microsoft/phi-4-gguf","6edc2ef6664b739a8e11e62f2672ff6afe0c15ac","MIT");
    public static readonly LocalModelSpec GptOss=new("gpt-oss","OpenAI gpt-oss-20b","gpt-oss-20b-MXFP4.gguf",12109566624,"27cd6c432c7672cb812a92f611cf3ba7bbc35928262bb1e1253ff4ee6ae35901","ggml-org/gpt-oss-20b-GGUF","15dcfc7e7c608c6161507211ed43c5b590801b4b","Apache-2.0");
    public static readonly LocalModelSpec Embedding=new("nomic","Nomic Embed Text v1.5","nomic-embed-text-v1.5.Q8_0.gguf",146146432,"3e24342164b3d94991ba9692fdc0dd08e3fd7362e0aacc396a9a5c54a544c3b7","nomic-ai/nomic-embed-text-v1.5-GGUF","0188c9bf409793f810680a5a431e7b899c46104c","Apache-2.0");
    public static readonly LocalModelSpec Reasoning14=new("ministral-reasoning-14","Mistral Ministral 3 14B Reasoning","Ministral-3-14B-Reasoning-2512-Q4_K_M.gguf",8239591488,"fe08ca2158cd7438211ec6a4e5256d31bc980f016e3f5b635fe91fe6848d461c","mistralai/Ministral-3-14B-Reasoning-2512-GGUF","650da2e5ed9a23b6d760d998bd8232ba6787b1bb","Apache-2.0");
    public static readonly LocalModelSpec Reasoning8=new("ministral-reasoning-8","Mistral Ministral 3 8B Reasoning","Ministral-3-8B-Reasoning-2512-Q4_K_M.gguf",5198910368,"894aa3645ef8708a81dbe201c26105ce37c4c741252c89c5a78f81b49ac438c6","mistralai/Ministral-3-8B-Reasoning-2512-GGUF","ab77f02a3bd3d21d31448e6a63a92e9c1e481673","Apache-2.0");
    public static readonly LocalModelSpec[] TextModels=[Reasoning14,GptOss,Reasoning8,Phi,Ministral];

    public static async Task<LocalModelSpec> VerifyTextModelAsync(string path, CancellationToken ct = default)
    {
        SafeFileOperations.NoLinks(path);
        var file = new FileInfo(path);
        var candidates = TextModels.Where(model => model.Bytes == file.Length).ToArray();
        if(candidates.Length == 0)throw new InvalidDataException("Select a verified model from ASTRA's non-Chinese publisher catalog. Unknown or modified weights are not enabled.");
        using var stream = File.OpenRead(path);
        string digest = Convert.ToHexString(await System.Security.Cryptography.SHA256.HashDataAsync(stream,ct));
        return candidates.FirstOrDefault(model => model.Sha256.Equals(digest,StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidDataException("The selected GGUF does not match a verified non-Chinese catalog model. No inference was started.");
    }
}
