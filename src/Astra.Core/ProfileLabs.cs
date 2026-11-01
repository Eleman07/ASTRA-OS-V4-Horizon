using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
namespace AstraOS.Core;
public sealed record GamingSample(DateTimeOffset Time,double? Cpu,double? Gpu,double? Ram);
public sealed record GamingSummary(DateTimeOffset Started,DateTimeOffset Ended,int Samples,double? CpuMean,double? CpuPeak,double? GpuMean,double? GpuPeak,double? RamPeak);
public static class GamingAnalysis
{
    public static GamingSummary Summarize(IReadOnlyList<GamingSample> samples)
    {
        if(samples.Count==0)throw new InvalidDataException(AstraOS.Core.UiText.T("Avvia una registrazione prima di analizzarla."));
        double? Stat(Func<GamingSample,double?> select,bool peak=false){var values=samples.Select(select).Where(v=>v.HasValue&&double.IsFinite(v.Value)).Select(v=>v!.Value).ToArray();return values.Length==0?null:peak?values.Max():values.Average();}
        return new(samples[0].Time,samples[^1].Time,samples.Count,Stat(s=>s.Cpu),Stat(s=>s.Cpu,true),Stat(s=>s.Gpu),Stat(s=>s.Gpu,true),Stat(s=>s.Ram,true));
    }
    public static string Csv(IEnumerable<GamingSample> samples)=>"timestamp,cpu_system_percent,gpu_busiest_engine_percent,ram_percent\n"+string.Join("\n",samples.Select(s=>string.Join(",",s.Time.ToString("O"),s.Cpu?.ToString("G6",CultureInfo.InvariantCulture),s.Gpu?.ToString("G6",CultureInfo.InvariantCulture),s.Ram?.ToString("G6",CultureInfo.InvariantCulture))));
}
public sealed record SubtitleCue(TimeSpan Start,TimeSpan End,string Text);
public static class SubtitleTimeline
{
    private static readonly Regex Timing=new(@"\A(?<h>\d{1,3}):(?<m>\d{2}):(?<s>\d{2})[,.](?<ms>\d{3})\z");
    public static IReadOnlyList<SubtitleCue> Parse(string text)
    {
        if(text.Length>2_000_000)throw new InvalidDataException(AstraOS.Core.UiText.T("Sottotitoli oltre 2 MB."));var result=new List<SubtitleCue>();
        TimeSpan Stamp(string value){var m=Timing.Match(value.Trim());if(!m.Success)throw new InvalidDataException(AstraOS.Core.UiText.T("Timestamp SRT non valido."));int Part(string key)=>int.Parse(m.Groups[key].Value,CultureInfo.InvariantCulture);if(Part("m")>59||Part("s")>59)throw new InvalidDataException(AstraOS.Core.UiText.T("Timestamp SRT non valido."));return TimeSpan.FromHours(Part("h"))+TimeSpan.FromMinutes(Part("m"))+TimeSpan.FromSeconds(Part("s"))+TimeSpan.FromMilliseconds(Part("ms"));}
        foreach(var block in Regex.Split(text.Trim().TrimStart('\uFEFF').Replace("\r\n","\n"),@"\n\s*\n"))
        {
            var lines=block.Split('\n');int i=lines.Length>0&&int.TryParse(lines[0],out _)?1:0;if(lines.Length<i+2)throw new InvalidDataException(AstraOS.Core.UiText.T("Blocco SRT incompleto."));var times=lines[i].Split("-->",StringSplitOptions.TrimEntries);if(times.Length!=2)throw new InvalidDataException(AstraOS.Core.UiText.T("Intervallo SRT mancante."));var start=Stamp(times[0]);var end=Stamp(times[1]);if(end<=start)throw new InvalidDataException(AstraOS.Core.UiText.T("Fine sottotitolo precedente all'inizio."));result.Add(new(start,end,string.Join("\n",lines.Skip(i+1))));if(result.Count>10000)throw new InvalidDataException(AstraOS.Core.UiText.T("Massimo 10.000 sottotitoli."));
        }
        return result;
    }
    public static IReadOnlyList<SubtitleCue> Retime(IEnumerable<SubtitleCue> cues,double offsetMs,double speed)
    {
        if(!double.IsFinite(offsetMs)||Math.Abs(offsetMs)>3_600_000||!double.IsFinite(speed)||speed is <.5 or >2)throw new InvalidDataException(AstraOS.Core.UiText.T("Offset massimo ±1 ora; scala temporale 0,5–2."));
        return cues.Select(c=>new SubtitleCue(TimeSpan.FromMilliseconds(Math.Max(0,c.Start.TotalMilliseconds*speed+offsetMs)),TimeSpan.FromMilliseconds(Math.Max(0,c.End.TotalMilliseconds*speed+offsetMs)),c.Text)).Where(c=>c.End>c.Start).ToArray();
    }
    public static string Export(IEnumerable<SubtitleCue> cues)
    {
        string Stamp(TimeSpan t)=>$"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00},{t.Milliseconds:000}";
        return string.Join("\n\n",cues.Select((c,i)=>$"{i+1}\n{Stamp(c.Start)} --> {Stamp(c.End)}\n{c.Text}"))+"\n";
    }
}
