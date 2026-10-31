namespace AstraOS.Core.Agents;

public sealed record LocalModelSpec(string Id,string Label,string FileName,long Bytes,string Sha256,string Repository,string Revision,string License)
{
    public string Url=>$"https://huggingface.co/{Repository}/resolve/{Revision}/{FileName}";
    public string Path=>System.IO.Path.Combine(AstraPaths.UserDataRoot,"ai","models",FileName);
}

/// <summary>Reviewed, revision-pinned open weights. No hosted inference or paid API is involved.</summary>
public static class LocalModelCatalog
{
    public static readonly LocalModelSpec Ministral=new("ministral","Mistral Ministral 3 8B","Ministral-3-8B-Instruct-2512-Q4_K_M.gguf",5198911904,"33e7a72cf5e6e2cfc2f2847075acc013d68bba023e35310cef86b5cf8fdca761","mistralai/Ministral-3-8B-Instruct-2512-GGUF","0102285ad796bd99af90f58de616092e5630e970","Apache-2.0");
    public static readonly LocalModelSpec Phi=new("phi4","Microsoft Phi-4 14B","phi-4-Q4_K.gguf",9053114560,"5652b9be0ea4ae2842130d04fe31bc869fcb99a2b7106c53b4e754a343fd688f","microsoft/phi-4-gguf","6edc2ef6664b739a8e11e62f2672ff6afe0c15ac","MIT");
    public static readonly LocalModelSpec GptOss=new("gpt-oss","OpenAI gpt-oss-20b","gpt-oss-20b-MXFP4.gguf",12109566624,"27cd6c432c7672cb812a92f611cf3ba7bbc35928262bb1e1253ff4ee6ae35901","ggml-org/gpt-oss-20b-GGUF","15dcfc7e7c608c6161507211ed43c5b590801b4b","Apache-2.0");
    public static readonly LocalModelSpec Embedding=new("nomic","Nomic Embed Text v1.5","nomic-embed-text-v1.5.Q8_0.gguf",146146432,"3e24342164b3d94991ba9692fdc0dd08e3fd7362e0aacc396a9a5c54a544c3b7","nomic-ai/nomic-embed-text-v1.5-GGUF","0188c9bf409793f810680a5a431e7b899c46104c","Apache-2.0");
    public static readonly LocalModelSpec[] TextModels=[Ministral,Phi,GptOss];
}
