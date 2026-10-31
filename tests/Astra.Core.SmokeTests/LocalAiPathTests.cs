using AstraOS.Core;

internal static class LocalAiPathTests
{
    public static void Run()
    {
        void Check(bool value,string name){if(!value)throw new Exception("FAIL: "+name);Console.WriteLine("PASS: "+name);}
        string root=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"astra-ai-relocated"));
        var old=LocalAiPaths.Resolve(root,@"C:\old\AstraOS-Local\Ai\components\llama-server.exe",@"C:\old\AstraOS-Local\Ai\components\astra-local-model.gguf");
        Check(old.Embedded&&old.Runtime==Path.Combine(root,"LLamaSharp.dll")&&old.Model==Path.Combine(root,"Ai","components","astra-local-model.gguf"),"Moved bundled server and model migrate together without requiring old files");
        var moved=LocalAiPaths.Resolve(root,@"D:\previous copy\LLamaSharp.dll",old.Model);
        Check(moved.Runtime==old.Runtime&&moved.Model==old.Model,"Integrated runtime settings follow a relocated portable application");
        var custom=LocalAiPaths.Resolve(root,@"C:\tools\llama-server.exe",@"D:\models\my-model.gguf");
        Check(!custom.Embedded&&custom.Runtime==@"C:\tools\llama-server.exe"&&custom.Model==@"D:\models\my-model.gguf","Custom runtime and model are preserved even when unavailable");
        var sameName=LocalAiPaths.Resolve(root,null,@"D:\models\astra-local-model.gguf");
        Check(sameName.Model==@"D:\models\astra-local-model.gguf","A similarly named model outside the bundled layout remains custom");
        var reset=LocalAiPaths.Resolve(root,null,null);
        Check(reset==old,"Use included AI resolves both defaults within the current application");
        var quoted=LocalAiPaths.Resolve(root,'"'+@"C:\OLD\AI\COMPONENTS\LLAMA-SERVER.EXE"+'"','"'+@"C:\OLD\AI\COMPONENTS\ASTRA-LOCAL-MODEL.GGUF"+'"');
        Check(quoted==reset,"Quoted and case-varied bundled settings migrate correctly");
    }
}
