using System.Text;
using LLama;
using LLama.Common;
using LLama.Native;
using LLama.Sampling;

namespace AstraOS.Desktop.Ai;

/// <summary>CPU inference through a versioned NuGet library. No server or listening port.</summary>
internal sealed class EmbeddedLlm : IDisposable
{
    private readonly SemaphoreSlim _gate=new(1,1);
    private readonly CancellationTokenSource _lifetime=new();
    private LLamaWeights? _weights;
    private ModelParams? _parameters;
    private static readonly object NativeGate=new();
    private static bool _configured;
    private int _disposed;
    public bool IsLoaded=>_weights is not null&&Volatile.Read(ref _disposed)==0;
    internal static void ConfigureNative()
    {
        lock(NativeGate)
        {
            if(_configured)return;
            string variant=System.Runtime.Intrinsics.X86.Avx2.IsSupported?"avx2":System.Runtime.Intrinsics.X86.Avx.IsSupported?"avx":"noavx";
            string folder=Path.Combine(AppContext.BaseDirectory,"runtimes","win-x64","native",variant);
            NativeLibraryConfig.All.WithLibrary(Path.Combine(folder,"llama.dll"),Path.Combine(folder,"mtmd.dll"));_configured=true;
        }
    }
    public async Task LoadAsync(string model,CancellationToken ct,uint contextSize=8192,int? inferenceThreads=null)
    {
        using var linked=CancellationTokenSource.CreateLinkedTokenSource(ct,_lifetime.Token);
        await _gate.WaitAsync(linked.Token);
        try
        {
            ConfigureNative();
            int threads=inferenceThreads??AstraOS.Core.PerformancePolicy.InferenceThreads(SystemSnapshot.Capture().PhysicalMemoryMb,Environment.ProcessorCount);
            _parameters=new ModelParams(model){GpuLayerCount=0,ContextSize=contextSize,Threads=Math.Max(1,threads),BatchThreads=Math.Max(1,threads)};
            _weights=await LLamaWeights.LoadFromFileAsync(_parameters,linked.Token);
            linked.Token.ThrowIfCancellationRequested();
        }
        catch{_weights?.Dispose();_weights=null;throw;}
        finally{_gate.Release();}
    }
    public async Task<string> CompleteAsync(string system,string input,int maxTokens,bool json,CancellationToken ct)
    {
        using var linked=CancellationTokenSource.CreateLinkedTokenSource(ct,_lifetime.Token);
        await _gate.WaitAsync(linked.Token);
        try
        {
            if(!IsLoaded||_parameters is null)throw new InvalidOperationException(AstraOS.Core.UiText.T("Il modello locale non è pronto."));
            return await Task.Run(async()=>
            {
                var executor=new StatelessExecutor(_weights!,_parameters){ApplyTemplate=true,SystemMessage=system};
                using var sampling=new DefaultSamplingPipeline{Temperature=0.1f,Seed=42,Grammar=json?new Grammar(JsonGrammar,"root"):null};
                var parameters=new InferenceParams{MaxTokens=Math.Max(1,maxTokens),SamplingPipeline=sampling,AntiPrompts=new[]{"<|im_end|>","<|endoftext|>"}};
                var output=new StringBuilder();
                await foreach(string part in executor.InferAsync(input,parameters,linked.Token))
                {
                    output.Append(part);
                }
                string answer=System.Text.RegularExpressions.Regex.Replace(output.ToString(),@"<think>[\s\S]*?</think>","").Trim();
                if(answer.Length==0)throw new InvalidDataException(AstraOS.Core.UiText.T("Il modello non ha prodotto una risposta utilizzabile."));return answer;
            },linked.Token);
        }
        finally{_gate.Release();}
    }
    // JSON syntax is constrained here; tool-specific validators still check semantics.
    public async Task<float[]> EmbedAsync(string text,CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if(_weights is null||_parameters is null)throw new InvalidOperationException("Local model is not loaded.");
            return await Task.Run(async()=>
            {
                var options=new ModelParams(_parameters.ModelPath){ContextSize=2048,Embeddings=true,PoolingType=LLamaPoolingType.Last,Threads=_parameters.Threads,GpuLayerCount=0};
                using var embedder=new LLamaEmbedder(_weights,options);
                var vectors=await embedder.GetEmbeddings(text,ct);
                var vector=vectors.Last();double norm=Math.Sqrt(vector.Sum(v=>(double)v*v));
                if(!double.IsFinite(norm)||norm<=0)throw new InvalidDataException("Invalid model embedding.");
                return vector.Select(v=>(float)(v/norm)).ToArray();
            },ct);
        }
        finally{_gate.Release();}
    }

    private const string JsonGrammar="""
        root ::= ws object ws
        object ::= "{" ws (string ws ":" ws value (ws "," ws string ws ":" ws value)*)? ws "}"
        array ::= "[" ws (value (ws "," ws value)*)? ws "]"
        value ::= object | array | string | number | "true" | "false" | "null"
        string ::= "\"" ([^"\\\x00-\x1f] | "\\" (["\\/bfnrt] | "u" [0-9a-fA-F] [0-9a-fA-F] [0-9a-fA-F] [0-9a-fA-F]))* "\""
        number ::= "-"? ("0" | [1-9] [0-9]*) ("." [0-9]+)? ([eE] [+-]? [0-9]+)?
        ws ::= [ \t\n\r]*
        """;
    public void Dispose()
    {
        if(Interlocked.Exchange(ref _disposed,1)!=0)return;
        _lifetime.Cancel();
        // Do not free native weights while a generation is still using them.
        _=Task.Run(async()=>{await _gate.WaitAsync();try{_weights?.Dispose();_weights=null;}finally{_gate.Release();_lifetime.Dispose();}});
    }
}
