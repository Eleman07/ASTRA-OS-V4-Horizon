using AstraOS.Core;

internal static class AgentWorkspaceTests
{
    private static void Check(bool value,string text){if(!value)throw new Exception("FAIL: "+text);Console.WriteLine("PASS: "+text);}
    public static void Run(string repository)
    {
        var skills=Directory.EnumerateFiles(Path.Combine(repository,"src","Astra.Desktop","AssistantSkills"),"*.json").Select(p=>AgentSkillRules.Import(File.ReadAllText(p))).ToArray();
        Check(skills.Length>=26&&skills.Select(s=>s.Id).Distinct().Count()==skills.Length,"Installable skill catalog has distinct validated packages");
        var markdown=AgentSkillRules.FromMarkdown("Imported skill","# Skill\nReview supplied code. Do not execute.");Check(markdown.Capabilities.SequenceEqual(new[]{"chat"}),"Markdown skills import as prompt-only guidance");
        try{AgentSkillRules.Import(AgentSkillRules.Export(markdown).Replace("\"chat\"","\"arbitrary-shell\""));throw new Exception("Unknown capability accepted");}catch(InvalidDataException){}
        string root=Path.Combine(Path.GetTempPath(),"astra-agent-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try
        {
            var store=new AgentSkillStore(Path.Combine(root,"skills"));store.Install(markdown);Check(store.Load().Single().Instructions==markdown.Instructions,"Installed skill survives encrypted storage and roundtrip");store.Remove(markdown);Check(store.Load().Count==0,"User-installed skill can be removed without executing instructions");
            var chats=new AssistantChatStore(Path.Combine(root,"chats"));var chat=chats.Begin("private-history-marker","creator");
            for(int i=0;i<120;i++)chats.Append(chat.Id,new AssistantMessage(i%2==0?"user":"assistant","private-message-marker "+i,DateTimeOffset.UtcNow));
            Check(chats.ReadPage(chat.Id,0,30).Last().Text.EndsWith("119")&&chats.ReadPage(chat.Id,90,30).First().Text.EndsWith(" 0"),"Encrypted chat pagination retains messages beyond the active context window");
            Check(!Directory.EnumerateFiles(Path.Combine(root,"chats"),"*.dpapi",SearchOption.AllDirectories).Any(p=>System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(p)).Contains("private-message-marker")),"Conversation text is not stored in plaintext");
            var longChat=chats.Begin("Long input and output fixture","stem");
            string longInput="INPUT_START\n"+new string('u',140000)+"\nINPUT_END_MARKER",longOutput="OUTPUT_START\n"+new string('a',160000)+"\nOUTPUT_END_MARKER";
            var initialTime=DateTimeOffset.UtcNow;
            chats.Append(longChat.Id,new AssistantMessage("user",longInput,initialTime));
            chats.Append(longChat.Id,new AssistantMessage("assistant",longOutput,initialTime.AddSeconds(1)));
            chats.Append(longChat.Id,new AssistantMessage("user","FOLLOWUP_END_MARKER",initialTime.AddSeconds(2)));
            var fullHistory=chats.ReadAll(longChat.Id);
            Check(fullHistory.Count==3&&fullHistory[0].Role=="user"&&fullHistory[0].Text==longInput&&fullHistory[1].Role=="assistant"&&fullHistory[1].Text==longOutput&&fullHistory[2].Text=="FOLLOWUP_END_MARKER"&&fullHistory.Select(m=>m.Time).SequenceEqual(fullHistory.Select(m=>m.Time).Order()),"Full encrypted chat history preserves messages beyond 100k characters in chronological order");
            string project=SafeFileOperations.CreateFolder(root,"project"),file=Path.Combine(project,"sample.py");File.WriteAllText(file,"value = 1\n");
            var plan=WorkspaceChangePlan.Prepare(project,new[]{new ProposedFileEdit("sample.py","value = 2\n")});
            Check(File.ReadAllText(file)=="value = 1\n"&&WorkspaceChangePlan.Preview(plan).Contains("+ value = 2"),"Preparing and reviewing a proposal does not write project files");
            File.WriteAllText(file,"value = 3\n");try{WorkspaceChangePlan.Apply(plan,Path.Combine(root,"backups"));throw new Exception("Stale plan applied");}catch(IOException){}
            Check(File.ReadAllText(file)=="value = 3\n","A stale proposal cannot overwrite an externally changed file");
            plan=WorkspaceChangePlan.Prepare(project,new[]{new ProposedFileEdit("sample.py","value = 4\n"),new ProposedFileEdit("new.txt","new file")});
            string backup=WorkspaceChangePlan.Apply(plan,Path.Combine(root,"backups"));Check(File.ReadAllText(file)=="value = 4\n"&&File.Exists(Path.Combine(project,"new.txt")),"Approved change application writes only the prepared project targets");
            WorkspaceChangePlan.Restore(project,backup);Check(File.ReadAllText(file)=="value = 3\n"&&!File.Exists(Path.Combine(project,"new.txt")),"Encrypted backup restoration recovers originals and removes its own new file");
            foreach(string path in new[]{"../escape.txt",@"C:\escape.txt",@"nested\..\escape.txt"}){try{WorkspaceChangePlan.Prepare(project,new[]{new ProposedFileEdit(path,"bad")});throw new Exception("Workspace escape accepted");}catch(InvalidDataException){}}
            Check(!File.Exists(Path.Combine(root,"escape.txt")),"Project changes cannot escape the selected workspace");
        }
        finally{if(Path.GetFileName(root).StartsWith("astra-agent-")&&Path.GetDirectoryName(root)==Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)){SafeFileOperations.NoLinks(root);Directory.Delete(root,true);}}
    }
}
