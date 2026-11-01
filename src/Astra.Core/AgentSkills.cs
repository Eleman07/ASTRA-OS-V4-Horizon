using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AstraOS.Core;

public sealed record AgentSkill(int SchemaVersion,string Id,string Name,string Description,string Category,string Instructions,string[] Capabilities,int PreferredTask=0);
public static class AgentSkillRules
{
    public static readonly string[] Capabilities={"chat","memory","web.search","web.read","project.read","project.propose","project.apply","process.run","study","stem","creator","gaming"};
    public static void Validate(AgentSkill skill)
    {
        if(skill is null||skill.SchemaVersion!=1||!Regex.IsMatch(skill.Id??"",@"^[a-z][a-z0-9.-]{2,79}$",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100))||string.IsNullOrWhiteSpace(skill.Name)||skill.Name.Length>100||skill.Name.Any(char.IsControl)||skill.Description is null||skill.Description.Length>600||string.IsNullOrWhiteSpace(skill.Instructions)||skill.Category is null||skill.Category.Length>50||skill.Capabilities is null||skill.Capabilities.Length>12||skill.Capabilities.Any(c=>!Capabilities.Contains(c))||skill.PreferredTask is <0 or >8)throw new InvalidDataException("Skill non valida o con capacità non supportate.");
    }
    public static AgentSkill Import(string json)
    {

        var skill=JsonSerializer.Deserialize<AgentSkill>(json,new JsonSerializerOptions{PropertyNameCaseInsensitive=true,UnmappedMemberHandling=System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,MaxDepth=8})??throw new InvalidDataException("Skill vuota.");Validate(skill);return skill;
    }
    public static string Export(AgentSkill skill){Validate(skill);return JsonSerializer.Serialize(skill,new JsonSerializerOptions{WriteIndented=true});}
    public static AgentSkill FromMarkdown(string name,string content)
    {
        string description="Imported prompt skill; actions still require the normal tool workflow.";
        if(content.StartsWith("---",StringComparison.Ordinal))
        {
            int end=content.IndexOf("\n---",3,StringComparison.Ordinal);
            if(end>0)
            {
                string front=content[..end];
                string? Field(string key){var match=Regex.Match(front,"(?m)^"+key+@":\s*([^\r\n]+)",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100));return match.Success?match.Groups[1].Value.Trim().Trim('"','\''):null;}
                name=Field("name")??name;description=Field("description")??description;
                content=content[(end+4)..].TrimStart('\r','\n');
            }
        }
        var skill=new AgentSkill(1,"org.astra.user."+Guid.NewGuid().ToString("N"),name,description,"Personal",content,new[]{"chat"});Validate(skill);return skill;
    }
}
public sealed class AgentSkillStore
{
    private readonly string _directory;
    public AgentSkillStore(string? directory=null)=>_directory=directory??Path.Combine(AstraPaths.UserDataRoot,"assistant","skills");
    private string PathFor(string id)=>Path.Combine(_directory,Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(id)))+".dpapi");
    public IReadOnlyList<AgentSkill> Load()
    {
        var list=new List<AgentSkill>();if(!Directory.Exists(_directory))return list;
        foreach(var file in Directory.EnumerateFiles(_directory,"*.dpapi"))
        {
            if(new FileInfo(file).Length>100000)throw new InvalidDataException("Skill cifrata troppo grande.");
            var plain=ProtectedData.Unprotect(File.ReadAllBytes(file),null,DataProtectionScope.CurrentUser);
            try{list.Add(AgentSkillRules.Import(System.Text.Encoding.UTF8.GetString(plain)));}finally{CryptographicOperations.ZeroMemory(plain);}
        }
        return list.OrderBy(s=>s.Name).ToArray();
    }
    public void Install(AgentSkill skill)
    {
        AgentSkillRules.Validate(skill);var bytes=System.Text.Encoding.UTF8.GetBytes(AgentSkillRules.Export(skill));Directory.CreateDirectory(_directory);string target=PathFor(skill.Id),temp=target+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{File.WriteAllBytes(temp,ProtectedData.Protect(bytes,null,DataProtectionScope.CurrentUser));File.Move(temp,target,true);}finally{CryptographicOperations.ZeroMemory(bytes);if(File.Exists(temp))File.Delete(temp);}
    }
    public void Remove(AgentSkill skill){AgentSkillRules.Validate(skill);File.Delete(PathFor(skill.Id));}
}
