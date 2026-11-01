using AstraOS.Core;
namespace AstraOS.Core.Agents;
public static class AgentSkillDefaults
{
    public static void InstallMissing(AgentRuntimeSettings settings)
    {
        var map=new Dictionary<string,string[]>{
            ["coordinator"]=["planning","knowledge-deliver"],
            ["core"]=["system-workflow","automation","docs","knowledge-ingest"],
            ["study"]=["study-pack","tutor","recall","math"],
            ["research"]=["research-evidence","research","experiment","compare"],
            ["engineering"]=["stem-deliverable","python","tests","units","code-review"],
            ["aerospace"]=["orbit-verification","gnc","math","units"],
            ["gaming"]=["gaming-lab","strategy","game-debrief","game-setup"],
            ["creator"]=["creator-pack","brief","storyboard","subtitles","accessibility","delivery"],
            ["reviewer"]=["precision-evidence","code-review","knowledge-audit","agent-improvement"]};
        foreach(var entry in map)if(!settings.SkillAssignments.ContainsKey(entry.Key))settings.SkillAssignments[entry.Key]=entry.Value.Select(id=>id.StartsWith("knowledge-",StringComparison.Ordinal)?"org.astra.knowledge."+id[10..]:"org.astra.skill."+id).ToArray();
    }
    public static string Instructions(AgentRuntimeSettings settings,Specialist agent)
    {
        if(!settings.SkillAssignments.TryGetValue(agent.Id,out var assigned))return "";
        string directory=Path.Combine(AppContext.BaseDirectory,"AssistantSkills");
        var bundled=Directory.Exists(directory)?Directory.EnumerateFiles(directory,"*.json").Select(file=>AgentSkillRules.Import(File.ReadAllText(file))):Enumerable.Empty<AgentSkill>();
        var installed=bundled.Concat(new AgentSkillStore().Load()).GroupBy(skill=>skill.Id).Select(group=>group.Last());
        return string.Join("\n\n",installed.Where(skill=>assigned.Contains(skill.Id)).Select(skill=>skill.Name+": "+skill.Instructions));
    }
}
