using System.Text.RegularExpressions;

namespace AstraOS.Core;

public sealed record AssistantAction(string Kind,string Argument);
public sealed record AssistantRoutine(string Id,string Name,string Description,IReadOnlyList<AssistantAction> Steps);
public static class AssistantWorkspace
{
    public static string Name(string workspace)=>"ASTRA Assistant";
    public static readonly AssistantRoutine[] Routines=
    {
        new("study","Sprint di studio","Obiettivo, memoria e concentrazione.",new[]{new AssistantAction("navigate","brain"),new("task","Ripassare i concetti e verificare le fonti"),new("focus","25")}),
        new("research","Ricerca documentata","Raccogli fonti, confronta e salva una sintesi.",new[]{new AssistantAction("navigate","brain"),new("task","Raccogliere almeno tre fonti e verificarne la data"),new("focus","30")}),
        new("creator","Consegna Creator","Checklist di pubblicazione e revisione degli asset.",new[]{new AssistantAction("navigate","creator"),new("task","Verificare crediti, licenze, sottotitoli e formati di consegna"),new("focus","45")}),
        new("gaming","Sessione Gaming","Prepara una sessione e un confronto ripetibile.",new[]{new AssistantAction("navigate","gaming"),new("task","Confrontare la stessa scena e annotare impostazioni e risultati"),new("focus","60")}),
        new("stem","Revisione STEM","Ipotesi, unità, risultati e riproducibilità.",new[]{new AssistantAction("navigate","stem"),new("task","Verificare unità, condizioni iniziali e limiti del modello"),new("focus","40")})
    };
    public static void Validate(IEnumerable<AssistantAction> actions)
    {
        var steps=actions.ToArray();if(steps.Length is <1 or >12)throw new InvalidDataException("Una routine richiede da 1 a 12 azioni.");
        foreach(var step in steps)
        {
            if(step.Argument is null||step.Argument.Length>300||step.Argument.Any(char.IsControl))throw new InvalidDataException("Argomento routine non valido.");
            bool valid=step.Kind switch
            {
                "navigate"=>NavigationCatalog.Routes.Contains(step.Argument),
                "focus"=>int.TryParse(step.Argument,out int minutes)&&minutes is >=1 and <=240,
                "task" or "remember"=>!string.IsNullOrWhiteSpace(step.Argument)&&step.Argument.Length<=120,
                _=>false
            };
            if(!valid)throw new InvalidDataException("Azione routine non supportata.");
        }
    }
    public static AssistantAction? Parse(string input)
    {
        if(string.IsNullOrWhiteSpace(input)||input.Length>1500)return null;
        string text=input.Trim();
        foreach(var (pattern,kind) in new[]{
            (@"^(?:cerca (?:sul web|web|online)|search web|web search|research)\s+(.+)$","web-search"),
            (@"^(?:leggi (?:pagina|url)|read (?:page|url))\s+(https://\S+)$","web-read"),
            (@"^(?:prepara routine|prepare routine|routine)\s+(study|research|creator|gaming|stem)$","routine"),
            (@"^(?:piano studio|study plan)\s+(.+)$","study"),
            (@"^(?:quiz|flashcard|flashcards)\s+(.+)$","quiz")})
        {
            var match=Regex.Match(text,pattern,RegexOptions.IgnoreCase|RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100));
            if(match.Success)return new(kind,match.Groups[1].Value);
        }
        return text.ToLowerInvariant() switch
        {
            "analizza gaming" or "gaming coach"=>new("gaming-coach",""),
            "verifica consegna" or "creator audit"=>new("creator-audit",""),
            "contesto" or "context"=>new("context",""),
            "centro controllo" or "control center"=>new("navigate","system"),
            _=>null
        };
    }
    public static string Guidance(string workspace)=>workspace switch
    {
        "gaming"=>"Help with gaming setup, accessibility, strategy from supplied rules, session review and measured telemetry. Never invent FPS, temperatures or game state. Do not suggest cheating, memory injection or disabling protection.",
        "creator"=>"Help with briefs, shot lists, original concepts, asset metadata, accessibility, subtitles, licensing checklists and delivery. Distinguish actual file metadata from creative suggestions. Do not claim to have watched media when only metadata or OCR was supplied.",
        "stem"=>"Help with study, code, simulations, units, derivations and reproducible experiments. State assumptions, cite supplied evidence and distinguish educational solvers from validated engineering analysis.",
        _=>"Help with research, documents, tasks, accessibility and everyday productivity. Use only supplied context and tools that have actually returned results."
    };
}
