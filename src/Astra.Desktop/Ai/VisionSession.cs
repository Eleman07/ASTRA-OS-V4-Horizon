using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LLama;
using LLama.Common;
using LLama.Native;
using LLama.Sampling;
using AstraOS.Core;

namespace AstraOS.Desktop.Ai;

/// <summary>Real pixel input through local MTMD inference, separate from Windows OCR.</summary>
internal static class VisionSession
{
    private static readonly SemaphoreSlim Gate=new(1,1);
    public static async Task<string> DescribeAsync(byte[] image,string goal,string modelPath,string projectorPath,int tokens,CancellationToken ct)
    {
        await Gate.WaitAsync(ct);
        try
        {
            string directory=Path.Combine(AppContext.BaseDirectory,"Ai","vision");
            string model=string.IsNullOrWhiteSpace(modelPath)?Path.Combine(directory,"vision-model.gguf"):Path.GetFullPath(modelPath);
            string projector=string.IsNullOrWhiteSpace(projectorPath)?Path.Combine(directory,"vision-projector.gguf"):Path.GetFullPath(projectorPath);
            if(!File.Exists(model)||!File.Exists(projector))throw new FileNotFoundException("Vision requires the local model and matching projector. Restore Ai/vision or select a compatible GGUF pair.");
            SafeFileOperations.NoLinks(model);SafeFileOperations.NoLinks(projector);
            if(string.IsNullOrWhiteSpace(modelPath)&&string.IsNullOrWhiteSpace(projectorPath))
            {
                using var manifest=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(directory,"manifest.json"),ct));
                foreach(var name in new[]{"vision-model.gguf","vision-projector.gguf"})
                {
                    using var file=File.OpenRead(Path.Combine(directory,name));
                    string hash=Convert.ToHexString(await SHA256.HashDataAsync(file,ct));
                    if(!hash.Equals(manifest.RootElement.GetProperty("files").GetProperty(name).GetProperty("sha256").GetString(),StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Vision model checksum mismatch.");
                }
            }
            if(GpuRuntime.Available)
            {
                await GpuRuntime.VerifyAsync(ct);using var local=new LocalLlmSession();
                await local.StartAsync(GpuRuntime.Executable,model,true,ct,4096,null,projector);
                return await local.AnalyzeImageAsync(image,"Describe the visible image accurately. Identify its main shapes, colors and readable labels. Distinguish observations from guesses.",tokens,ct);
            }
            throw new InvalidOperationException("GPU vision requires the included Vulkan runtime and an updated GPU driver. Restore Ai/gpu and retry; CPU fallback is disabled.");
        }
        finally{Gate.Release();}
    }
}
