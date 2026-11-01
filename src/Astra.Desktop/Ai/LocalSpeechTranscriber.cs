using Whisper.net;
using Whisper.net.LibraryLoader;
using AstraOS.Core.Agents;

namespace AstraOS.Desktop.Ai;

public sealed record SpeechSegment(TimeSpan Start,TimeSpan End,string Text)
{ public string? Language { get; init; } }

/// <summary>Offline speech recognition using only the pinned OpenAI Whisper checkpoint and CPU speech runtime.</summary>
internal sealed class LocalSpeechTranscriber : IDisposable
{
    private readonly WhisperFactory _factory;
    private readonly WhisperProcessor _processor;
    public string ModelLabel { get; }
    private LocalSpeechTranscriber(string model,string language)
    {
        ModelLabel=Path.GetFileName(model)=="ggml-small.bin"?"OpenAI Whisper small":"OpenAI Whisper tiny";
        RuntimeOptions.RuntimeLibraryOrder=[RuntimeLibrary.Cpu];
        _factory=WhisperFactory.FromPath(model,new WhisperFactoryOptions{UseGpu=false});
        try
        {
            var builder=_factory.CreateBuilder().WithThreads(Math.Max(1,Environment.ProcessorCount/2)).WithNoContext().WithTemperature(0);
            if(language=="auto")builder.WithLanguageDetection();
            else if(language.Length==2&&language.All(char.IsAsciiLetter))builder.WithLanguage(language.ToLowerInvariant());
            else throw new ArgumentException("Choose automatic speech-language detection or a supported two-letter Whisper language code.");
            builder.WithBeamSearchSamplingStrategy(strategy=>strategy.WithBeamSize(5));
            _processor=builder.Build();
        }
        catch{_factory.Dispose();throw;}
    }
    public static async Task<LocalSpeechTranscriber> CreateAsync(CancellationToken ct,string? language=null)=>new(await BundledAi.VerifyWhisperAsync(ct),language??new AgentJobStore().Configuration.SpeechLanguage);
    public async Task<IReadOnlyList<SpeechSegment>> ProcessChunkAsync(Stream wave,TimeSpan offset,CancellationToken ct)
    {
        var result=new List<SpeechSegment>();
        await foreach(var segment in _processor.ProcessAsync(wave,ct))
        {
            ct.ThrowIfCancellationRequested();
            result.Add(new(offset+segment.Start,offset+segment.End,segment.Text.Trim()){Language=segment.Language});
        }
        return result;
    }
    public void Dispose(){_processor.Dispose();_factory.Dispose();}
}
