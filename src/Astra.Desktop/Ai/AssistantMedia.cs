using System.Globalization;
using System.Speech.Synthesis;
using AstraOS.Core;
using AstraOS.Core.Agents;
using Microsoft.UI.Xaml.Media.Imaging;
using NAudio.Wave;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;
using Windows.Storage.Streams;

namespace AstraOS.Desktop.Ai;
public sealed record TextBoxRegion(string Text,double X,double Y,double Width,double Height);
public sealed record AssistantInput(string Name,string Kind,string Text,IReadOnlyList<SpeechSegment>? Segments=null,SoftwareBitmapSource? Image=null,IReadOnlyList<TextBoxRegion>? Regions=null,int Width=0,int Height=0)
{ public byte[]? Pixels { get; init; } }

public static class AssistantMedia
{
    public static readonly string[] Extensions={".txt",".md",".log",".json",".csv",".tsv",".py",".cs",".js",".html",".css",".xml",".yaml",".yml",".ipynb",".tex",".pdf",".docx",".xlsx",".pptx",".odt",".ods",".odp",".epub",".png",".jpg",".jpeg",".bmp",".gif",".tif",".tiff",".webp",".wav",".mp3",".m4a",".aac",".flac",".ogg",".glb",".gltf",".obj",".stl"};
    private static readonly SemaphoreSlim SpeechGate=new(1,1);
    public static async Task<AssistantInput> ReadAsync(string path,CancellationToken ct)
    {
        var info=new FileInfo(path);string extension=info.Extension.ToLowerInvariant();if(!info.Exists)throw new FileNotFoundException("Selected input file no longer exists.",path);
        if(new[]{".wav",".mp3",".m4a",".aac",".flac",".ogg"}.Contains(extension))return await TranscribeAsync(path,ct);
        if(new[]{".png",".jpg",".jpeg",".bmp",".gif",".tif",".tiff",".webp"}.Contains(extension))
        {using var stream=await (await StorageFile.GetFileFromPathAsync(path)).OpenReadAsync();return await ReadImageAsync(stream,info.Name,ct);}
        if(new[]{".glb",".gltf",".obj",".stl"}.Contains(extension)){var model=await Task.Run(async()=>MeshImport.Parse(info.Name,await File.ReadAllBytesAsync(path,ct)),ct);return new(info.Name,"model",$"{model.Name}\nFormat: {model.Format}\nVertices: {model.Vertices}\nTriangles: {model.Triangles}\nGeometry alone does not specify physical properties or boundary conditions.");}
        var document=await Task.Run(()=>DocumentTextReader.Read(path,ct),ct);
        return new(info.Name,document.Kind,document.Text);
    }
    public static async Task<AssistantInput> ReadImageAsync(IRandomAccessStream stream,string name,CancellationToken ct)
    {
        var decoder=await BitmapDecoder.CreateAsync(stream);double scale=Math.Min(1,Math.Min((double)OcrEngine.MaxImageDimension/decoder.PixelWidth,(double)OcrEngine.MaxImageDimension/decoder.PixelHeight));
        var transform=new BitmapTransform{ScaledWidth=(uint)Math.Max(1,decoder.PixelWidth*scale),ScaledHeight=(uint)Math.Max(1,decoder.PixelHeight*scale)};
        using var bitmap=await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Ignore,transform,ExifOrientationMode.RespectExifOrientation,ColorManagementMode.DoNotColorManage);ct.ThrowIfCancellationRequested();
        var engine=OcrEngine.TryCreateFromUserProfileLanguages()??OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language("en-US"));
        var result=engine is null?null:await engine.RecognizeAsync(bitmap);var regions=result?.Lines.SelectMany(l=>l.Words.Select(w=>new TextBoxRegion(w.Text,w.BoundingRect.X,w.BoundingRect.Y,w.BoundingRect.Width,w.BoundingRect.Height))).ToArray()??Array.Empty<TextBoxRegion>();
        var image=new SoftwareBitmapSource();using var preview=SoftwareBitmap.Convert(bitmap,BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied);await image.SetBitmapAsync(preview);
        using var fullBitmap=await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Ignore);
        using var encoded=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,encoded);encoder.SetSoftwareBitmap(fullBitmap);await encoder.FlushAsync();encoded.Seek(0);using var reader=new DataReader(encoded.GetInputStreamAt(0));await reader.LoadAsync((uint)encoded.Size);var pixels=new byte[(int)encoded.Size];reader.ReadBytes(pixels);
        return new(name,"image",result?.Text??"Windows OCR language unavailable; image pixels remain available for visual analysis with the selected provider.",null,image,regions,bitmap.PixelWidth,bitmap.PixelHeight){Pixels=pixels};
    }
    public static Task<string> DescribeImageAsync(byte[] pixels,string goal,CancellationToken ct)
    {
        var settings=new AgentJobStore().Configuration;
        if(settings.UseCodex)return CodexBridge.DescribeImageAsync(pixels,goal,settings,ct);
        return settings.PreferFreeCloud?LocalLlmSession.AnalyzeCloudMediaAsync(pixels,"image","png",goal,ct):VisionSession.DescribeAsync(pixels,goal,settings.VisionModelPath,settings.VisionProjectorPath,0,ct);
    }
    public static async Task<AssistantInput> TranscribeAsync(string path,CancellationToken ct)
    {
        await SpeechGate.WaitAsync(ct);
        try
        {
            var speechSettings=new AgentJobStore().Configuration;
            bool online=speechSettings.PreferFreeCloud&&!speechSettings.UseCodex;
            using var processor=online?null:await LocalSpeechTranscriber.CreateAsync(ct);
            var segments=new List<SpeechSegment>();
            using var reader=new MediaFoundationReader(path);
            using var resampler=new MediaFoundationResampler(reader,new WaveFormat(16000,16,1)){ResamplerQuality=40};
            var buffer=new byte[32000];long offset=0;bool finished=false;
            while(!finished)
            {
                ct.ThrowIfCancellationRequested();using var chunk=new MemoryStream();long count=0;
                using(var writer=new WaveFileWriter(chunk,resampler.WaveFormat))
                {
                    while(count<9_600_000){int read=resampler.Read(buffer,0,(int)Math.Min(buffer.Length,9_600_000-count));if(read==0){finished=true;break;}writer.Write(buffer,0,read);count+=read;}
                }
                if(count==0)break;
                TimeSpan start=TimeSpan.FromSeconds(offset/32000.0),end=TimeSpan.FromSeconds((offset+count)/32000.0);
                if(online)
                {
                    string transcript=await LocalLlmSession.AnalyzeCloudMediaAsync(chunk.ToArray(),"audio","wav","Transcribe the audio faithfully in its original language. Return only the transcript. Mark unclear words [inaudible]. Do not summarize or invent missing speech.",ct);
                    segments.Add(new(start,end,transcript));
                }
                else
                {
                    using var audio=new MemoryStream(chunk.ToArray(),writable:false);
                    var recognized=await processor!.ProcessChunkAsync(audio,start,ct);
                    segments.AddRange(recognized);
                    if(recognized.Count==0)segments.Add(new(start,end,"[No speech detected in this audio chunk; inspect the recording.]"));
                }
                offset+=count;
            }
            string Stamp(TimeSpan value)=>$"{(int)value.TotalHours:00}:{value.Minutes:00}:{value.Seconds:00}.{value.Milliseconds:000}";
            return new(Path.GetFileName(path),"audio",(online?"OpenRouter audio transcription; provider interpretation and chunk-level timestamps.":"Local "+processor!.ModelLabel+" transcription on CPU; estimated segment timestamps. Review recognition errors, especially numbers and technical terms, before reuse.")+"\n"+string.Join("\n",segments.Select(s=>$"[{Stamp(s.Start)}–{Stamp(s.End)}]\n{s.Text}")),segments);
        }
        finally{SpeechGate.Release();}
    }
    public static string ToSrt(IEnumerable<SpeechSegment> segments)
    {
        string Stamp(TimeSpan t)=>$"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00},{t.Milliseconds:000}";
        return string.Join("\n\n",segments.Select((s,i)=>$"{i+1}\n{Stamp(s.Start)} --> {Stamp(s.End)}\n{s.Text}"));
    }
    public static Task<byte[]> SynthesizeAsync(string text,CancellationToken ct)=>Task.Run(async()=>
    {
        if(string.IsNullOrWhiteSpace(text))throw new ArgumentException("Voice output text is empty.");
        using var synthesizer=new SpeechSynthesizer();var voices=synthesizer.GetInstalledVoices().Where(v=>v.Enabled).ToArray();var culture=CultureInfo.GetCultureInfo(UiText.Language);var voice=voices.FirstOrDefault(v=>v.VoiceInfo.Culture.TwoLetterISOLanguageName==culture.TwoLetterISOLanguageName);if(voice is not null)synthesizer.SelectVoice(voice.VoiceInfo.Name);
        using var output=new MemoryStream();synthesizer.SetOutputToWaveStream(output);var completed=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);synthesizer.SpeakCompleted+=(_,e)=>{if(e.Error is not null)completed.TrySetException(e.Error);else if(e.Cancelled)completed.TrySetCanceled();else completed.TrySetResult();};using var registration=ct.Register(synthesizer.SpeakAsyncCancelAll);ct.ThrowIfCancellationRequested();synthesizer.SpeakAsync(text);await completed.Task.WaitAsync(ct);return output.ToArray();
    },ct);
}

public sealed class AssistantRecorder:IDisposable
{
    private WaveInEvent? _input;private WaveFileWriter? _writer;private string? _path;
    private TaskCompletionSource<string>? _stopped;
    public bool IsRecording=>_input is not null;
    public void Start()
    {
        if(IsRecording)return;string directory=Path.Combine(AstraPaths.UserDataRoot,"assistant","audio");Directory.CreateDirectory(directory);_path=Path.Combine(directory,"recording-"+Guid.NewGuid().ToString("N")+".wav");
        _input=new WaveInEvent{WaveFormat=new WaveFormat(16000,16,1),BufferMilliseconds=100};_writer=new WaveFileWriter(_path,_input.WaveFormat);_stopped=new(TaskCreationOptions.RunContinuationsAsynchronously);
        _input.DataAvailable+=(_,e)=>_writer?.Write(e.Buffer,0,e.BytesRecorded);
        _input.RecordingStopped+=(_,e)=>{_writer?.Dispose();_writer=null;if(e.Exception is not null)_stopped.TrySetException(e.Exception);else _stopped.TrySetResult(_path);};
        try{_input.StartRecording();}catch{Dispose();throw;}
    }
    public async Task<string> StopAsync(){var input=_input??throw new InvalidOperationException(AstraOS.Core.UiText.T("Registrazione non attiva."));input.StopRecording();var path=await _stopped!.Task;input.Dispose();_input=null;_path=null;return path;}
    public void Dispose(){try{_input?.StopRecording();}catch(Exception){}_input?.Dispose();_input=null;_writer?.Dispose();_writer=null;if(_path is not null){try{File.Delete(_path);}catch(IOException){}catch(UnauthorizedAccessException){}_path=null;}}
}
