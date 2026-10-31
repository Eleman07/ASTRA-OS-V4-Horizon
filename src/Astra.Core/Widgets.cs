using System.Text.Json;

namespace AstraOS.Core;

public sealed record WidgetSpec
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Title { get; init; } = "Widget";
    public string Kind { get; init; } = "clock";
    public string Text { get; init; } = "";
    public string Color { get; init; } = "theme";
    public int Width { get; init; } = 1;
    public string TimeZone { get; init; } = "local";
    public DateTimeOffset? Target { get; init; }
    public AutomationSpec? Automation { get; init; }
}
public sealed record AutomationSpec
{
    public bool Enabled { get; init; }
    public string Trigger { get; init; } = "interval";
    public int IntervalMinutes { get; init; } = 30;
    public int Hour { get; init; } = 9;
    public int Minute { get; init; }
    public string Action { get; init; } = "notify";
    public string Argument { get; init; } = "";
    public DateTimeOffset NextDue { get; init; } = DateTimeOffset.UtcNow.AddMinutes(30);
}
public sealed record WidgetState(int SchemaVersion,IReadOnlyList<WidgetSpec> Widgets);

public static class WidgetRules
{
    public static readonly string[] Kinds = { "clock", "system", "note", "countdown", "focus", "tasks", "workspace", "automation" };
    public static readonly string[] Actions = { "notify", "navigate", "focus", "note", "routine" };
    public const int MaxWidgets = 32;

    public static void Validate(WidgetSpec w)
    {
        if (w is null || !Guid.TryParseExact(w.Id,"N",out _) || string.IsNullOrWhiteSpace(w.Title) || w.Title.Length>80 || w.Title.Any(char.IsControl) ||
            !Kinds.Contains(w.Kind) || w.Text is null || w.Text.Length>2000 || w.Width is <1 or >2 ||
            w.Color is not("theme" or "ice" or "snow")) throw new InvalidDataException(AstraOS.Core.UiText.T("Configurazione widget non valida."));
        if (w.TimeZone!="local")
        {
            if (w.TimeZone is null || w.TimeZone.Length>100) throw new InvalidDataException(AstraOS.Core.UiText.T("Fuso orario non valido."));
            try { _=TimeZoneInfo.FindSystemTimeZoneById(w.TimeZone); }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException) { throw new InvalidDataException(AstraOS.Core.UiText.T("Fuso orario non disponibile."),ex); }
        }
        if(w.Automation is not {} a)return;
        if(a.Trigger is not("interval" or "daily") || a.IntervalMinutes is <1 or >10080 || a.Hour is <0 or >23 || a.Minute is <0 or >59 ||
            !Actions.Contains(a.Action) || a.Argument is null || a.Argument.Length>1500) throw new InvalidDataException(AstraOS.Core.UiText.T("Automazione non valida."));
        if(a.Action=="navigate" && !NavigationCatalog.Routes.Contains(a.Argument))throw new InvalidDataException(AstraOS.Core.UiText.T("Destinazione dell'automazione non valida."));
        if(a.Action=="focus" && (!int.TryParse(a.Argument,out var duration)||duration is <1 or >180))throw new InvalidDataException(AstraOS.Core.UiText.T("Durata focus: 1–180 minuti."));
        if(a.Action=="routine"&&!AssistantWorkspace.Routines.Any(r=>r.Id==a.Argument)&&!Guid.TryParseExact(a.Argument,"N",out _))throw new InvalidDataException("Routine non valida.");
        if(a.Action is "notify" or "note" && string.IsNullOrWhiteSpace(a.Argument))throw new InvalidDataException(AstraOS.Core.UiText.T("Inserisci il testo dell'automazione."));
    }
    public static DateTimeOffset Next(AutomationSpec a,DateTimeOffset now,TimeZoneInfo zone)
    {
        if(a.Trigger=="interval")return now.AddMinutes(a.IntervalMinutes);
        DateTime local=TimeZoneInfo.ConvertTime(now,zone).Date.AddHours(a.Hour).AddMinutes(a.Minute);
        for(int day=0;day<4;day++,local=local.AddDays(1))
        {
            var adjusted=local;
            while(zone.IsInvalidTime(adjusted))adjusted=adjusted.AddMinutes(1);
            var offset=zone.IsAmbiguousTime(adjusted)?zone.GetAmbiguousTimeOffsets(adjusted).Min():zone.GetUtcOffset(adjusted);
            var candidate=new DateTimeOffset(DateTime.SpecifyKind(adjusted,DateTimeKind.Unspecified),offset);
            if(candidate>now)return candidate;
        }
        throw new InvalidOperationException(AstraOS.Core.UiText.T("Pianificazione non disponibile."));
    }
    public static IReadOnlyList<WidgetSpec> Due(IEnumerable<WidgetSpec> widgets,DateTimeOffset now)=>widgets.Where(w=>w.Automation is { Enabled:true } a && a.NextDue<=now).ToArray();
    public static string Export(IEnumerable<WidgetSpec> widgets)
    {
        var list=widgets.ToArray();if(list.Length>MaxWidgets)throw new InvalidDataException(AstraOS.Core.UiText.T("Massimo 32 widget."));foreach(var w in list)Validate(w);
        return JsonSerializer.Serialize(new WidgetState(1,list),new JsonSerializerOptions{WriteIndented=true});
    }
    public static IReadOnlyList<WidgetSpec> Import(string json)
    {
        if(json.Length>128000)throw new InvalidDataException(AstraOS.Core.UiText.T("Pacchetto widget oltre 128 KB."));
        var state=JsonSerializer.Deserialize<WidgetState>(json,new JsonSerializerOptions{MaxDepth=8,UnmappedMemberHandling=System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow});
        if(state?.SchemaVersion!=1||state.Widgets is null||state.Widgets.Count>MaxWidgets||state.Widgets.Any(w=>w is null)||state.Widgets.Select(w=>w.Id).Distinct().Count()!=state.Widgets.Count)throw new InvalidDataException(AstraOS.Core.UiText.T("Pacchetto widget non valido."));
        foreach(var w in state.Widgets)Validate(w);return state.Widgets;
    }
    public static IReadOnlyList<WidgetSpec> Defaults()=>new[]{new WidgetSpec{Title=AstraOS.Core.UiText.T("Orologio"),Kind="clock"},new WidgetSpec{Title=AstraOS.Core.UiText.T("Sistema"),Kind="system"},new WidgetSpec{Title=AstraOS.Core.UiText.T("Il mio spazio"),Kind="note",Text=AstraOS.Core.UiText.T("Idee e obiettivi del giorno")},new WidgetSpec{Title=AstraOS.Core.UiText.T("Focus"),Kind="focus",Text="25"}};
}
