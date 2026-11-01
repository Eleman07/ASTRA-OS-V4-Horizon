using System.Text.RegularExpressions;

namespace AstraOS.Core.SecondBrain;
public sealed record BrainCommand(string Kind,string Argument);
public static class BrainCommands
{
    public static BrainCommand? Parse(string input)
    {
        if(string.IsNullOrWhiteSpace(input)||input.Length>300)return null;
        string text=Regex.Replace(input.Trim().TrimEnd('.','!','?'),@"\s+"," ");string key=text.ToLowerInvariant();
        if(key is "aiuto" or "help" or "cosa puoi fare")return new("help","");
        if(key is "briefing" or "riepilogo giornata" or "daily briefing")return new("briefing","");
        if(key is "riassumi pdf" or "riassumi il pdf" or "riassumi documento" or "summarize pdf" or "summarize document")return new("pdf-summary","");
        if(key is "annulla pdf" or "cancel pdf")return new("cancel-pdf","");
        if(key is "apri chrome" or "open chrome" or "apri google chrome" or "open google chrome")return new("chrome","");
        var timer=Regex.Match(key,@"^(?:focus|timer|avvia focus|start focus)(?: per| for)? (\d{1,3})(?: minut[io]| minutes?)?$");
        if(timer.Success&&int.TryParse(timer.Groups[1].Value,out int minutes)&&minutes is >=1 and <=240)return new("focus",minutes.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var task=Regex.Match(text,@"^(?:aggiungi attività|aggiungi attivita|ricordami di|add task)\s+(.+)$",RegexOptions.IgnoreCase);
        if(task.Success&&task.Groups[1].Value.Length<=120&&!task.Groups[1].Value.Any(char.IsControl))return new("task",task.Groups[1].Value);
        var memory=Regex.Match(text,@"^(?:ricorda che|remember that)\s+(.+)$",RegexOptions.IgnoreCase);
        if(memory.Success)return new("remember",memory.Groups[1].Value);
        var open=Regex.Match(text,@"^(?:apri|open)\s+([\p{L}\p{N} .+#-]{1,100})$",RegexOptions.IgnoreCase);
        if(open.Success)return new("open",open.Groups[1].Value);
        return null;
    }
}
