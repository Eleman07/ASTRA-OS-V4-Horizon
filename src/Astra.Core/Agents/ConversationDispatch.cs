namespace AstraOS.Core.Agents;

public sealed record ConversationRoute(bool UseTeam, string[] Roles, string Reason);

/// <summary>Routes the explicit user request only; attached text never authorizes actions.</summary>
public static class ConversationDispatch
{
    public static ConversationRoute Plan(string goal, string mode = "auto", IEnumerable<string>? selected = null, IEnumerable<string>? previous = null)
    {
        string text = goal.ToLowerInvariant();
        var manual = (selected ?? []).Where(id => AgentCatalog.All.Any(a => a.Id == id && id is not "coordinator" and not "reviewer")).Distinct().ToArray();
        var roles = manual.Length > 0 ? manual : AgentCatalog.Route(goal);
        if(manual.Length==0 && roles.Contains("study") && System.Text.RegularExpressions.Regex.IsMatch(text,@"codice\s+(?:\d|di calibrazione|identificativo|fiscale)") && !new[]{"python","software","sorgente","algoritm"}.Any(text.Contains))roles=roles.Where(r=>r!="engineering").ToArray();
        bool followup = new[] { "aggiungi", "modifica", "correggi", "estendi", "aggiorna", "risolvi", "continua", "prosegui", "procedi", "riprova", "approfondisci", "continue", "go on", "fix ", "extend ", "update ", "add " }.Any(text.StartsWith);
        if (manual.Length == 0 && followup && previous?.Any() == true) roles = previous.Distinct().ToArray();
        bool deliverable = new[] { "realizza", "sviluppa", "implementa", "progetta", "progetto", "simulatore", "simulazione d'esame", "flashcard", "mappa mentale", "ricerca scientifica", "pacchetto", "storyboard", "build ", "develop ", "implement ", "research ", "create a project", "technical project", "test di", "requisiti", "requirements", "analisi comparativa" }.Any(text.Contains);
        deliverable |= new[]{"crea","genera","scrivi","costruisci","generate","write","create"}.Any(text.Contains) && (roles.Any(r=>r!="core")||goal.Length>240);
        bool coordinated = mode == "team" || mode != "direct" && (deliverable || followup || roles.Length > 1 || manual.Length > 0);
        return new(coordinated, roles, coordinated ? "Specialisti, skills assegnate e revisione delle evidenze" : "Risposta diretta con il metodo dello specialista pertinente");
    }
}
