using System.Text.Json;
using AstraOS.Desktop.Ai;
using NAudio.Wave;

string root=Directory.GetCurrentDirectory();
string fixture=Path.GetFullPath(args.FirstOrDefault()??Path.Combine(root,"qa-cloud-media-4.2.0","calibration.wav"));
if(!File.Exists(fixture))throw new FileNotFoundException("Supply an existing synthetic calibration WAV.",fixture);
string report=Path.Combine(root,"logs","local-provider-tests","local-audio-smoke.json");
Directory.CreateDirectory(Path.GetDirectoryName(report)!);
using var cancellation=new CancellationTokenSource(TimeSpan.FromMinutes(3));
string languageSetting=args.ElementAtOrDefault(1)??"auto";
using var speech=await LocalSpeechTranscriber.CreateAsync(cancellation.Token,languageSetting);
async Task<IReadOnlyList<SpeechSegment>> Recognize(TimeSpan offset)
{
    using var reader=new MediaFoundationReader(fixture);
    using var resampler=new MediaFoundationResampler(reader,new WaveFormat(16000,16,1)){ResamplerQuality=40};
    using var pcm=new MemoryStream();var buffer=new byte[32000];
    using(var writer=new WaveFileWriter(pcm,resampler.WaveFormat))
    {
        int read;while((read=resampler.Read(buffer,0,buffer.Length))>0)writer.Write(buffer,0,read);
    }
    using var wave=new MemoryStream(pcm.ToArray(),writable:false);
    return await speech.ProcessChunkAsync(wave,offset,cancellation.Token);
}
var first=await Recognize(TimeSpan.Zero);
var second=await Recognize(TimeSpan.FromHours(25));
bool returnedSpeech=first.Count>0&&first.All(segment=>segment.End>=segment.Start)&&first.Any(segment=>!string.IsNullOrWhiteSpace(segment.Text));
bool longOffsetPreserved=second.Count>0&&second.All(segment=>segment.Start>=TimeSpan.FromHours(25)&&segment.End>=segment.Start)&&second.Any(segment=>!string.IsNullOrWhiteSpace(segment.Text));
string manifest=Path.Combine(AppContext.BaseDirectory,"Ai","components","manifest.json"),original=File.ReadAllText(manifest);bool changedManifestRejected=false;
try
{
    File.WriteAllText(manifest,original.Replace("1be3a9b2063867b937e64e2ec7483364a79917e157fa98c5d94b5c1fffea987b",new string('0',64),StringComparison.Ordinal));
    try{await BundledAi.VerifyWhisperAsync(cancellation.Token);}catch(InvalidDataException){changedManifestRejected=true;}
}
finally{File.WriteAllText(manifest,original);}
string transcript=string.Join(" ",first.Select(segment=>segment.Text));
string normalized=transcript.ToLowerInvariant().Replace("nine","9").Replace("five","5").Replace("seven","7").Replace("three","3").Replace("one","1");
bool? calibrationAccuracyPassed=Path.GetFileName(fixture)=="calibration.wav"?new string(normalized.Where(char.IsDigit).ToArray()).Contains("95731",StringComparison.Ordinal):null;
bool? italianReferenceWordsPassed=Path.GetFileName(fixture)=="qa-synthetic.wav"?new[]{"prova","audio","progetto","dinamica","orbitale"}.All(word=>transcript.Contains(word,StringComparison.OrdinalIgnoreCase)):null;
bool transportPassed=returnedSpeech&&longOffsetPreserved&&changedManifestRejected;
bool passed=transportPassed&&calibrationAccuracyPassed!=false&&italianReferenceWordsPassed!=false;
await File.WriteAllTextAsync(report,JsonSerializer.Serialize(new{passed,transportPassed,localInference=true,provider=speech.ModelLabel+" via Whisper.net CPU",languageSetting,hostedInferenceRequests=0,syntheticFixture=Path.GetFileName(fixture),returnedSpeech,longOffsetPreserved,changedManifestRejected,calibrationAccuracyPassed,italianReferenceWordsPassed,expectedCalibrationDigits=calibrationAccuracyPassed.HasValue?"95731":null,actualTranscript=transcript,segments=first,secondChunkAt25Hours=second},new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine(passed?"PASS: actual local Whisper inference, long chunk offsets, pinned-model manifest rejection and calibration accuracy when known.":"FAIL: local speech accuracy/transport smoke; inspect "+report);
if(!passed)Environment.ExitCode=1;
