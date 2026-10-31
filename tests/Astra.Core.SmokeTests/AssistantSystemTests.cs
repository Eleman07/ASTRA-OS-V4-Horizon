using AstraOS.Core;
using System.IO.Compression;
using System.Net;

internal static class AssistantSystemTests
{
    private static void Check(bool value,string name){if(!value)throw new Exception("FAIL: "+name);Console.WriteLine("PASS: "+name);}
    public static async Task RunAsync()
    {
        foreach(var routine in AssistantWorkspace.Routines)AssistantWorkspace.Validate(routine.Steps);
        Check(new[]{"core","stem","gaming","creator"}.All(p=>AssistantWorkspace.Name(p)=="ASTRA Assistant"),"One assistant identity and shared Second Brain across every profile");
        Check(AssistantWorkspace.Parse("cerca web ESA Ariane 6") is {Kind:"web-search",Argument:"ESA Ariane 6"},"Explicit research query preserves its topic");
        Check(AssistantWorkspace.Parse("routine creator") is {Kind:"routine"}&&AssistantWorkspace.Parse("run powershell delete everything")==null,"Assistant routes supported intents and rejects arbitrary execution");
        Check(NavigationCatalog.Resolve("control center")=="system"&&DesktopCapabilities.All.All(c=>c.Mode=="settings"?c.Target.StartsWith("ms-settings:"):c.Target=="search"||NavigationCatalog.Routes.Contains(c.Target)),"Control-center capabilities map to valid system or application destinations");
        foreach(string url in new[]{"http://example.com","https://127.0.0.1","https://192.168.1.1","https://[::1]","https://localhost.example.local","https://user:secret@example.com","https://example.com:8443","https://169.254.169.254","https://[::ffff:127.0.0.1]"})
        {try{PublicWebAddress.Parse(url);throw new Exception("Unsafe URL accepted: "+url);}catch(ArgumentException){}}
        Check(PublicWebAddress.Parse("https://example.com/article").Host=="example.com"&&PublicWebAddress.IsPublic(IPAddress.Parse("2606:4700:4700::1111"))&&!PublicWebAddress.IsPublic(IPAddress.Parse("100.64.0.1")),"Public web access excludes private networks, credentials and custom ports");
        try{AssistantWorkspace.Validate(new[]{new AssistantAction("shell","whoami")});throw new Exception("Shell accepted");}catch(InvalidDataException){}
        try{AssistantWorkspace.Validate(new[]{new AssistantAction("focus","0")});throw new Exception("Invalid focus accepted");}catch(InvalidDataException){}
        var scheduled=new WidgetSpec{Kind="automation",Automation=new AutomationSpec{Action="routine",Argument="creator",Enabled=false}};WidgetRules.Validate(scheduled);
        Check(!WidgetRules.Import(WidgetRules.Export(new[]{scheduled}))[0].Automation!.Enabled,"Assistant routines survive widget serialization without enabling themselves");
        string root=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"astra-system-"+Guid.NewGuid().ToString("N")));Directory.CreateDirectory(root);
        try
        {
            string original=SafeFileOperations.CreateFolder(root,"original");Directory.CreateDirectory(Path.Combine(original,"empty"));Directory.CreateDirectory(Path.Combine(original,"nested"));
            byte[] content=Enumerable.Range(0,2048).Select(i=>(byte)(i%251)).ToArray();await File.WriteAllBytesAsync(Path.Combine(original,"nested","sample.dat"),content);
            string copies=SafeFileOperations.CreateFolder(root,"copies");string copy=await SafeFileOperations.CopyAsync(original,copies);
            Check((await File.ReadAllBytesAsync(Path.Combine(copy,"nested","sample.dat"))).SequenceEqual(content)&&Directory.Exists(Path.Combine(copy,"empty")),"Folder copy preserves nested bytes and empty directories");
            try{await SafeFileOperations.CopyAsync(original,copies);throw new Exception("Overwrite allowed");}catch(IOException){}
            string cancelled=SafeFileOperations.CreateFolder(root,"cancelled");using var cancellation=new CancellationTokenSource();cancellation.Cancel();
            try{await SafeFileOperations.CopyAsync(original,cancelled,cancellation.Token);throw new Exception("Cancellation ignored");}catch(OperationCanceledException){}
            Check(!Directory.EnumerateFileSystemEntries(cancelled).Any()&&File.Exists(Path.Combine(original,"nested","sample.dat")),"Cancelled copy removes its own staging data and preserves originals");
            string zip=await SafeFileOperations.ZipFileAsync(original);string extracted=await SafeFileOperations.ExtractZipAsync(zip);
            Check((await File.ReadAllBytesAsync(Path.Combine(extracted,"nested","sample.dat"))).SequenceEqual(content),"ZIP folder roundtrip preserves original file bytes");
            foreach(var (name,entryName) in new[]{("slip","../escaped.txt"),("drive","C:/escaped.txt"),("device","NUL"),("ads","sample.txt:stream")})
            {
                string archive=Path.Combine(root,name+".zip");using(var archiveFile=ZipFile.Open(archive,ZipArchiveMode.Create)){using var writer=new StreamWriter(archiveFile.CreateEntry(entryName).Open());writer.Write("unsafe");}
                try{await SafeFileOperations.ExtractZipAsync(archive);throw new Exception("Archive escape accepted");}catch(Exception ex) when(ex is InvalidDataException or ArgumentException){}
                Check(!Directory.Exists(Path.Combine(root,name+"-extracted")),"Unsafe archive rejected before creating output: "+name);
            }
            Check(!File.Exists(Path.Combine(root,"escaped.txt")),"Archive traversal cannot write outside the destination");
            string renamed=SafeFileOperations.Rename(copy,"renamed");string moved=SafeFileOperations.Move(renamed,cancelled);
            Check(Directory.Exists(moved)&&!Directory.Exists(renamed),"Rename and move operate on the selected real directory");
            var store=new AssistantRoutineStore(Path.Combine(root,"private"));var personal=new AssistantRoutine(Guid.NewGuid().ToString("N"),"private-marker","local",new[]{new AssistantAction("task","private-marker")});store.Save(new[]{personal});
            Check(store.Load().Single().Steps[0].Argument=="private-marker"&&!System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(root,"private","routines.dpapi"))).Contains("private-marker"),"Custom assistant routines persist encrypted for the current account");
        }
        finally{if(root.StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase)&&Path.GetFileName(root).StartsWith("astra-system-")){SafeFileOperations.NoLinks(root);Directory.Delete(root,true);}}
    }
}
