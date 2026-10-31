using System.Diagnostics;
using System.Text.Json;
using AstraOS.Core.Agents;

var store=new AgentJobStore();Directory.CreateDirectory(store.Root);FileStream lease;
try{lease=new FileStream(Path.Combine(store.Root,"supervisor.lease"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}catch(IOException){return;}
using(lease)
{
    void Recover()
    {
        foreach(var failed in store.Records.Enumerate().Where(j=>j.Status=="Failed"&&!j.CancelRequested&&j.RepairAttempts<3))
            AgentRecoveryPolicy.TryRecover(store,failed);
    }
    while(store.Configuration.Enabled)
    {
        try
        {
            Recover();
            string executable=Path.Combine(AppContext.BaseDirectory,"Astra.AgentWorker.exe");
            if(!File.Exists(executable))throw new FileNotFoundException("Agent worker is missing; reinstall the complete verified ASTRA distribution.");
            using var worker=Process.Start(new ProcessStartInfo(executable){UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=AppContext.BaseDirectory})??throw new InvalidOperationException("Worker launch failed.");
            while(!worker.HasExited&&store.Configuration.Enabled)
            {Recover();File.WriteAllText(Path.Combine(store.Root,"supervisor.json"),JsonSerializer.Serialize(new{supervisorPid=Environment.ProcessId,workerPid=worker.Id,utc=DateTimeOffset.UtcNow}));await Task.Delay(2000);}
            if(!store.Configuration.Enabled){if(!worker.HasExited)await Task.WhenAny(worker.WaitForExitAsync(),Task.Delay(5000));if(!worker.HasExited)worker.Kill(true);break;}
            if(!store.Configuration.AutoRepair)break;
            await Task.Delay(3000);
        }
        catch(Exception ex){File.WriteAllText(Path.Combine(store.Root,"supervisor-error.txt"),ex.Message);await Task.Delay(15000);}
    }
    File.Delete(Path.Combine(store.Root,"supervisor.json"));
}
