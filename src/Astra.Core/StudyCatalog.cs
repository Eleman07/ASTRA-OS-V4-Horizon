namespace AstraOS.Core;

public sealed record StudyChoice(string Id,string Name);
public static class StudyCatalog
{
    public static IReadOnlyList<StudyChoice> Domains { get; } = new[]{new StudyChoice("general",AstraOS.Core.UiText.T("STEM · interdisciplinare")),new("engineering",AstraOS.Core.UiText.T("Ingegneria")),new("physics",AstraOS.Core.UiText.T("Fisica")),new("mathematics",AstraOS.Core.UiText.T("Matematica")),new("sciences",AstraOS.Core.UiText.T("Scienze"))};
    public static IReadOnlyList<StudyChoice> Engineering { get; } = new[]{
        new StudyChoice("aerospace",AstraOS.Core.UiText.T("Aerospaziale")),new("mechanical",AstraOS.Core.UiText.T("Meccanica")),new("electrical",AstraOS.Core.UiText.T("Elettrica")),new("electronics",AstraOS.Core.UiText.T("Elettronica")),
        new("computer",AstraOS.Core.UiText.T("Informatica")),new("automation",AstraOS.Core.UiText.T("Automazione e controllo")),new("robotics",AstraOS.Core.UiText.T("Robotica")),new("civil",AstraOS.Core.UiText.T("Civile e strutturale")),
        new("building",AstraOS.Core.UiText.T("Edile e architettura")),new("environmental",AstraOS.Core.UiText.T("Ambientale e territorio")),new("chemical",AstraOS.Core.UiText.T("Chimica e di processo")),
        new("biomedical",AstraOS.Core.UiText.T("Biomedica")),new("energy",AstraOS.Core.UiText.T("Energetica")),new("nuclear",AstraOS.Core.UiText.T("Nucleare")),new("materials",AstraOS.Core.UiText.T("Materiali")),new("naval",AstraOS.Core.UiText.T("Navale e oceanica")),
        new("industrial",AstraOS.Core.UiText.T("Industriale e produzione")),new("management",AstraOS.Core.UiText.T("Gestionale")),new("telecom",AstraOS.Core.UiText.T("Telecomunicazioni")),new("transport",AstraOS.Core.UiText.T("Trasporti")),
        new("geotechnical",AstraOS.Core.UiText.T("Geotecnica e mineraria")),new("agricultural",AstraOS.Core.UiText.T("Agraria e alimentare")),new("other",AstraOS.Core.UiText.T("Altro indirizzo / interdisciplinare"))};
    public static string Key(string domain,string track)=>domain=="engineering"?track:domain;
    public static string Name(string domain,string track)=>domain=="engineering"?"Ingegneria · "+Engineering.First(x=>x.Id==track).Name:Domains.First(x=>x.Id==domain).Name;
}
