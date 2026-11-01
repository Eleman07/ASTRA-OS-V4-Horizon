using System.Diagnostics;
using System.Text;
using AstraOS.Core;

namespace AstraOS.Desktop.Ai;
public sealed record ScriptResult(int ExitCode,string Output,bool Truncated);
public static class ProjectWorkspaceTools
{
    public static readonly string[] TextExtensions={".cs",".py",".js",".ts",".tsx",".jsx",".json",".md",".txt",".html",".css",".sql",".xml",".yml",".yaml",".toml",".ps1",".sh",".csv",".csproj"};
    public static IReadOnlyList<string> List(string root)
    {
        root=SafeFileOperations.DirectoryPath(root);var result=new List<string>();var pending=new Stack<string>();pending.Push(root);int visited=0;
        while(pending.TryPop(out var directory)&&visited++<3000&&result.Count<1000)
        {
            try
            {
                foreach(var path in Directory.EnumerateFileSystemEntries(directory))
                {
                    if((File.GetAttributes(path)&(FileAttributes.ReparsePoint|FileAttributes.System|FileAttributes.Hidden))!=0)continue;
                    if(Directory.Exists(path)){if(Path.GetFileName(path) is not("node_modules" or "bin" or "obj" or ".git" or ".venv" or "dist"))pending.Push(path);}
                    else if(TextExtensions.Contains(Path.GetExtension(path).ToLowerInvariant())&&result.Count<1000)result.Add(Path.GetRelativePath(root,path));
                }
            }catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){}
        }
        return result.OrderBy(x=>x).ToArray();
    }
    public static string Read(string root,string relative)
    {
        string path=WorkspaceChangePlan.Resolve(root,relative);if(!TextExtensions.Contains(Path.GetExtension(path).ToLowerInvariant())||new FileInfo(path).Length>1_000_000)throw new InvalidDataException("File di progetto non supportato o oltre 1 MB.");return File.ReadAllText(path,new UTF8Encoding(false,true));
    }
    public static string Interpreter(string root,string script)
    {
        string ext=Path.GetExtension(script).ToLowerInvariant();
        if(ext==".ps1")return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"WindowsPowerShell","v1.0","powershell.exe");
        string name=ext switch{".py"=>"python.exe",".js"=>"node.exe",_=>throw new InvalidDataException("Script supportati: Python, PowerShell e JavaScript.")};
        foreach(var directory in (Environment.GetEnvironmentVariable("PATH")??"").Split(Path.PathSeparator,StringSplitOptions.RemoveEmptyEntries))
        {
            if(!Path.IsPathRooted(directory))continue;string path=Path.GetFullPath(Path.Combine(directory.Trim('"'),name));
            if(!path.StartsWith(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)&&File.Exists(path))return path;
        }
        throw new FileNotFoundException("Interprete non installato: "+name);
    }
    public static async Task<ScriptResult> RunApprovedAsync(string root,string relative,string interpreter,IReadOnlyList<string> arguments,CancellationToken ct,TimeSpan? timeLimit=null)
    {
        string script=WorkspaceChangePlan.Resolve(root,relative);if(arguments.Count>32||arguments.Any(a=>a.Length>2000||a.Contains('\0')))throw new InvalidDataException("Argomenti script non validi.");
        var psi=new ProcessStartInfo(interpreter){WorkingDirectory=Path.GetFullPath(root),UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
        if(script.EndsWith(".ps1",StringComparison.OrdinalIgnoreCase)){psi.ArgumentList.Add("-NoProfile");psi.ArgumentList.Add("-NonInteractive");psi.ArgumentList.Add("-File");}
        psi.ArgumentList.Add(script);foreach(var argument in arguments)psi.ArgumentList.Add(argument);
        using var process=Process.Start(psi)??throw new InvalidOperationException("Avvio script non riuscito.");
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct);var limit=timeLimit??TimeSpan.FromMinutes(2);if(limit>TimeSpan.Zero)timeout.CancelAfter(limit);var result=new StringBuilder();bool truncated=false;var gate=new object();
        async Task Drain(StreamReader reader)
        {
            var buffer=new char[2048];int count;while((count=await reader.ReadAsync(buffer.AsMemory(),timeout.Token))>0)lock(gate){int remaining=64000-result.Length;if(remaining>0)result.Append(buffer,0,Math.Min(count,remaining));if(count>remaining)truncated=true;}
        }
        var output=Drain(process.StandardOutput);var error=Drain(process.StandardError);
        try{await process.WaitForExitAsync(timeout.Token);await Task.WhenAll(output,error);return new(process.ExitCode,result.ToString(),truncated);}
        finally{if(!process.HasExited)process.Kill(true);try{await Task.WhenAll(output,error);}catch(OperationCanceledException){}}
    }
}
