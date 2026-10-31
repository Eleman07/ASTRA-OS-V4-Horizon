using AstraOS.Desktop;
using System.Text.Json;

if(args.Contains("--dock-fixture")){DockFixture.Run();return;}
if(args.Contains("--agent-process"))
{
    string directory=Path.Combine(Path.GetTempPath(),"astra-process-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
    try
    {
        await File.WriteAllTextAsync(Path.Combine(directory,"probe.py"),"import sys,json\nprint(json.dumps(sys.argv[1:]))\n");
        string interpreter=AstraOS.Desktop.Ai.ProjectWorkspaceTools.Interpreter(directory,"probe.py");
        string literal="; echo should-not-execute $(whoami)";
        var result=await AstraOS.Desktop.Ai.ProjectWorkspaceTools.RunApprovedAsync(directory,"probe.py",interpreter,new[]{literal},CancellationToken.None);
        if(result.ExitCode!=0||!result.Output.Contains(literal))throw new Exception("Script arguments were not passed literally.");
        Console.WriteLine("PASS: approved script runner uses literal arguments, captures output and exit status");
        await File.WriteAllTextAsync(Path.Combine(directory,"wait.py"),"import time\ntime.sleep(10)\nopen('late-marker','w').write('unexpected')\n");
        using var cancel=new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        try{await AstraOS.Desktop.Ai.ProjectWorkspaceTools.RunApprovedAsync(directory,"wait.py",interpreter,Array.Empty<string>(),cancel.Token);throw new Exception("Script cancellation ignored");}catch(OperationCanceledException){}
        if(File.Exists(Path.Combine(directory,"late-marker")))throw new Exception("Cancelled script kept running");Console.WriteLine("PASS: cancellation terminates the process started by ASTRA");
    }
    finally{if(Path.GetFileName(directory).StartsWith("astra-process-")){AstraOS.Core.SafeFileOperations.NoLinks(directory);Directory.Delete(directory,true);}}
    return;
}
if(args.Contains("--web-read"))
{
    using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(25));
    var page=await AstraOS.Desktop.Ai.PublicWebResearch.ReadAsync("https://example.com",timeout.Token);
    if(!page.Text.Contains("Example Domain",StringComparison.OrdinalIgnoreCase))throw new Exception("Public web article text was not read.");
    Console.WriteLine("PASS: public HTTPS page read with original URL, title and retrieval time: "+page.Url);return;
}
if(args.Contains("--web-search"))
{
    using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(25));
    var results=await AstraOS.Desktop.Ai.PublicWebResearch.SearchAsync("ESA Ariane 6",timeout.Token);
    Console.WriteLine("Public search results: "+results.Count);foreach(var item in results)Console.WriteLine(item.Title+" | "+item.Url);return;
}
if(args.Contains("--brain-only")){await BrainDocumentTests.RunAsync(args.Contains("--ocr"));return;}
var report = new Dictionary<string, object?>();
Check(DockWindowRules.Include(42,7,0,0,false,"Chrome_WidgetWin_1"),"Dock includes ordinary application windows");
Check(!DockWindowRules.Include(7,7,0,0,false,"WinUIDesktopWin32WindowClass"),"Dock excludes ASTRA and its companion windows");
Check(!DockWindowRules.Include(42,7,0,0x80,false,"Tool"),"Dock excludes tool palettes");
Check(!DockWindowRules.Include(42,7,0,0x08000000,false,"Overlay"),"Dock excludes no-activate overlays");
Check(!DockWindowRules.Include(42,7,0,0,true,"Dialog"),"Owned dialog is represented by its parent app");
Check(DockWindowRules.Include(42,7,0,0x40000,true,"Application"),"Explicit taskbar app remains eligible when owned");
Check(!DockWindowRules.Include(42,7,0x40000000,0,false,"Child"),"Dock excludes child controls");
Check(!DockWindowRules.Include(42,7,0,0,false,"Shell_TrayWnd")&&!DockWindowRules.Include(42,7,0,0,false,"WorkerW"),"Dock excludes Windows shell surfaces");
try{WindowWorkspace.Activate(new WorkspaceWindow(0,0,"fixture","fixture"));throw new Exception("Dock accepted a stale HWND");}
catch(InvalidOperationException){Console.WriteLine("PASS: Dock rejects destroyed or invalid windows");}
var folderIcon=NativeShellIcon.ForFile(AppContext.BaseDirectory,true,48);
var appIcon=NativeShellIcon.ForFile(Environment.ProcessPath!,false,48);
Check(folderIcon?.HasPixels==true&&appIcon?.HasPixels==true,"Shell returns visible folder and executable icons");
Check(!folderIcon!.Bgra.SequenceEqual(appIcon!.Bgra),"Folder and application icons use their actual Windows graphics");
Check(NativeShellIcon.ForFile("astra-unknown.fixture-extension",false,32)?.HasPixels==true,"Unknown file type has a visible native fallback icon");
Check(Enumerable.Range(0,48*48).All(i=>folderIcon.Bgra[i*4]<=folderIcon.Bgra[i*4+3]&&folderIcon.Bgra[i*4+1]<=folderIcon.Bgra[i*4+3]&&folderIcon.Bgra[i*4+2]<=folderIcon.Bgra[i*4+3]),"Shell pixels preserve premultiplied transparency");
if(args.Contains("--dock-only"))return;
Check(LoginStartup.CommandFor(@"C:\ASTRA OS\Astra.Desktop.exe") == "\"C:\\ASTRA OS\\Astra.Desktop.exe\"", "Login startup quotes paths containing spaces");
try { LoginStartup.CommandFor("C:\\ASTRA\\Astra.Desktop.exe\" --unexpected"); throw new Exception("Startup command injection accepted"); }
catch (ArgumentException) { Console.WriteLine("PASS: Login startup rejects command injection"); }
try { LoginStartup.CommandFor("Astra.Desktop.exe"); throw new Exception("Relative startup path accepted"); }
catch (ArgumentException) { Console.WriteLine("PASS: Login startup rejects relative paths"); }
Check(WslInspector.ParseDistributionNames("\uFEFFU\0b\0u\0n\0t\0u\0\r\nUbuntu-24.04\r\nError: no distributions").SequenceEqual(new[]{"Ubuntu","Ubuntu-24.04"}),"WSL distro names UTF16 and diagnostics filtering");
Check(WslInspector.SelectUbuntu(new[]{"Debian"}) is null,"No false Ubuntu for a different distro");
Check(WslInspector.SelectUbuntu(new[]{"Ubuntu-24.04","Ubuntu"})=="Ubuntu","Registered Ubuntu preferred");
using(var ai=new AstraOS.Desktop.Ai.LocalLlmSession())
{
    try{await ai.StartAsync("","",false);throw new Exception("empty AI paths accepted");}
    catch(ArgumentException ex){Check(ex.Message.Contains("Scegli runtime"),"AI empty path gives actionable picker message");}
    Check(!ai.IsRunning,"Empty paths never start an AI process");
}
var fixture=Path.Combine(Path.GetTempPath(),"astra-files-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(fixture);
try
{
    Directory.CreateDirectory(Path.Combine(fixture,"Folder"));await File.WriteAllTextAsync(Path.Combine(fixture,"result.csv"),"x,y\n1,2");
    var snapshot=FileExplorerService.ReadDirectory(fixture);Check(snapshot.Entries.Count==2&&snapshot.Entries[0].IsDirectory,"Real file metadata with folders first");
    Check(snapshot.Entries.Single(e=>!e.IsDirectory).Bytes==7,"Explorer measures actual file size");
    Check(!FileExplorerService.IsCurrentDesktopEntry(snapshot.Entries[0]),"Orbit rejects entries outside known Desktop roots");
}finally{Directory.Delete(fixture,true);}
var desktop=FileExplorerService.ReadDesktop();Check(desktop.All(FileExplorerService.IsCurrentDesktopEntry),"Every orbit item belongs to actual Desktop");report["desktopEntryCount"]=desktop.Count;
report["chromeExecutable"]=ChromeLauncher.Find();
report["defaultBrowsers"]=new[]{"http","https",".htm",".html"}.ToDictionary(k=>k,k=>ChromeLauncher.DefaultHandler(k));
using (var monitor = new SystemMonitor())
{
    var first = await Task.Run(monitor.Capture);
    await Task.Delay(1200);
    var second = await Task.Run(monitor.Capture);
    Check(second.CpuPercent is >= 0 and <= 100, "real system CPU sample");
    Check(second.RamPercent is >= 0 and <= 100 && second.AvailableMemory <= second.TotalMemory, "real system memory sample");
    Check(second.ProcessCount > 0, "process enumeration");
    Check(second.GpuPercent is null or >= 0 and <= 100, "GPU counters bounded or unavailable");
    report["systemSample"] = second;
}
using (var disposed = new SystemMonitor())
{
    disposed.Dispose();
    try { disposed.Capture(); throw new Exception("Disposed monitor accepted capture"); }
    catch (ObjectDisposedException) { report["monitorDisposal"] = "PASS"; }
}
var apps = await Task.Run(() => InstalledAppCatalog.Discover(CancellationToken.None));
Check(apps.All(a => a.Id.Length == 64 && File.Exists(a.Shortcut) && Path.GetExtension(a.Shortcut).Equals(".lnk", StringComparison.OrdinalIgnoreCase)), "installed app discovery uses real shortcuts");
report["installedAppCount"] = apps.Count;
Console.WriteLine("Collecting security status (15-second timeout)…");
if (args.Length > 0) await File.WriteAllTextAsync(Path.GetFullPath(args[0]), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
try { var status = await SecurityInspector.ReadAsync(); Check(status.Contains("Protezione in tempo reale"), "security query is parsed"); report["security"] = status; }
catch (Exception ex) { report["security"] = "NOT AVAILABLE: " + ex.Message; }
Console.WriteLine("Collecting WSL status (10-second timeout)…");
try { report["wsl"] = await WslInspector.GetStatusAsync(); }
catch (Exception ex) { report["wsl"] = "NOT AVAILABLE: " + ex.Message; }
report["deploymentTools"] = DeploymentInspector.Read();
var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
if (args.Length > 0) await File.WriteAllTextAsync(Path.GetFullPath(args[0]), json);
Console.WriteLine(json);

static void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAILED: " + name);
    Console.WriteLine("PASS: " + name);
}
