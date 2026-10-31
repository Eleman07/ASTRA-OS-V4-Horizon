using System.Security.Cryptography;
using System.Text.Json;

namespace AstraOS.Core;

public sealed record AssistantConversation(string Id,string Title,string Workspace,DateTimeOffset Updated);
public sealed record AssistantMessage(string Role,string Text,DateTimeOffset Time);
/// <summary>Encrypted append-only message files: no daily/message quota; context windows stay bounded.</summary>
public sealed class AssistantChatStore
{
    private readonly string _root;
    public AssistantChatStore(string? root=null)=>_root=root??Path.Combine(AstraPaths.UserDataRoot,"assistant","conversations");
    private string Folder(string id){if(!Guid.TryParseExact(id,"N",out _))throw new InvalidDataException("Conversazione non valida.");return Path.Combine(_root,id);}
    private static void Write(string path,object value)
    {
        byte[] plain=JsonSerializer.SerializeToUtf8Bytes(value);string tmp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{Directory.CreateDirectory(Path.GetDirectoryName(path)!);File.WriteAllBytes(tmp,ProtectedData.Protect(plain,null,DataProtectionScope.CurrentUser));File.Move(tmp,path,true);}
        finally{CryptographicOperations.ZeroMemory(plain);if(File.Exists(tmp))File.Delete(tmp);}
    }
    private static T Read<T>(string path)
    {
        var plain=ProtectedData.Unprotect(File.ReadAllBytes(path),null,DataProtectionScope.CurrentUser);
        try{return JsonSerializer.Deserialize<T>(plain)??throw new InvalidDataException("Conversazione non leggibile.");}finally{CryptographicOperations.ZeroMemory(plain);}
    }
    public AssistantConversation Begin(string title,string workspace)
    {
        var chat=new AssistantConversation(Guid.NewGuid().ToString("N"),title[..Math.Min(title.Length,80)],workspace,DateTimeOffset.UtcNow);Write(Path.Combine(Folder(chat.Id),"header.dpapi"),chat);return chat;
    }
    public void Append(string id,AssistantMessage message)
    {
        if(message.Role is not("user" or "assistant"))throw new InvalidDataException("Messaggio non valido.");
        string folder=Folder(id);var header=Read<AssistantConversation>(Path.Combine(folder,"header.dpapi"));Write(Path.Combine(folder,DateTime.UtcNow.Ticks.ToString("D19")+"-"+Guid.NewGuid().ToString("N")+".dpapi"),message);Write(Path.Combine(folder,"header.dpapi"),header with{Updated=DateTimeOffset.UtcNow});
    }
    public IReadOnlyList<AssistantConversation> List(int skip=0,int take=50)
    {
        if(!Directory.Exists(_root))return Array.Empty<AssistantConversation>();
        return Directory.EnumerateDirectories(_root).Where(p=>Guid.TryParseExact(Path.GetFileName(p),"N",out _)).OrderByDescending(Directory.GetLastWriteTimeUtc).Skip(Math.Max(0,skip)).Take(Math.Clamp(take,1,100)).Select(p=>Read<AssistantConversation>(Path.Combine(p,"header.dpapi"))).ToArray();
    }
    public IReadOnlyList<AssistantMessage> ReadPage(string id,int skip=0,int take=30)=>Directory.EnumerateFiles(Folder(id),"*.dpapi").Where(p=>Path.GetFileName(p)!="header.dpapi").OrderByDescending(Path.GetFileName).Skip(Math.Max(0,skip)).Take(Math.Clamp(take,1,100)).Reverse().Select(Read<AssistantMessage>).ToArray();
    public IReadOnlyList<AssistantMessage> ReadAll(string id)=>Directory.EnumerateFiles(Folder(id),"*.dpapi").Where(p=>Path.GetFileName(p)!="header.dpapi").OrderBy(Path.GetFileName).Select(Read<AssistantMessage>).ToArray();
}
