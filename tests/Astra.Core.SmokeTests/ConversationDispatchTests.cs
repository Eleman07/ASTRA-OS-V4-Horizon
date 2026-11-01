using AstraOS.Core;
using AstraOS.Core.Agents;

internal static class ConversationDispatchTests
{
    public static void Run()
    {
        int count=0;
        void Check(bool okay,string name){if(!okay)throw new Exception("Conversation routing: "+name);count++;Console.WriteLine("PASS Conversation: "+name);}
        Check(!ConversationDispatch.Plan("Ciao").UseTeam,"greeting does not start a coordinated job");
        var orbital=ConversationDispatch.Plan("Realizza un progetto Python per simulare una missione CubeSat orbitale");
        Check(orbital.UseTeam&&orbital.Roles.Contains("aerospace")&&orbital.Roles.Contains("engineering"),"actual orbital objective automatically selects engineering and aerospace");
        Check(ConversationDispatch.Plan("Crea flashcards e simulazione d'esame").UseTeam,"study deliverables use specialists and review");
        Check(ConversationDispatch.Plan("Crea uno storyboard per un video").Roles.Contains("creator"),"creator delivery routes by the explicit objective");
        Check(!ConversationDispatch.Plan("Realizza un progetto", "direct").UseTeam,"user can explicitly choose direct answers");
        Check(ConversationDispatch.Plan("Spiegami", "team").UseTeam,"user can explicitly request coordination");
        var manual=ConversationDispatch.Plan("Analizza",selected:["research","not-a-role","coordinator"]);
        Check(manual.UseTeam&&manual.Roles.SequenceEqual(new[]{"research"}),"manual specialist selection is validated and preserved");
        var follow=ConversationDispatch.Plan("Aggiungi una verifica",previous:["engineering","aerospace"]);
        Check(follow.UseTeam&&follow.Roles.SequenceEqual(new[]{"engineering","aerospace"}),"follow-up keeps the preceding technical specialists");
        var continuation=ConversationDispatch.Plan("Continua",previous:["engineering","aerospace"]);Check(continuation.UseTeam&&continuation.Roles.SequenceEqual(new[]{"engineering","aerospace"}),"a short continuation preserves the technical team");
        var quick=ConversationDispatch.Plan("Quanto fa 2 + 2?");
        Check(!quick.UseTeam&&quick.Roles.SequenceEqual(new[]{"core"}),"a simple question stays a direct answer");
        Check(ConversationDispatch.Plan("Crea flashcards per lo studio e conserva il codice 75731").Roles.SequenceEqual(new[]{"study"}),"a calibration identifier does not incorrectly add a coding agent to study work");
        var software=ConversationDispatch.Plan("Genera un software di analisi");Check(software.UseTeam&&software.Roles.Contains("engineering"),"software generation is coordinated even without the word project");
        Check(RenderBudget.Select(4096,4,false,false).GraphNodes==120,"small-memory device gets a smaller render batch");
        Check(RenderBudget.Select(32768,16,false,false).GraphNodes==600,"larger hardware gets a larger initial visual batch");
        Check(RenderBudget.Select(32768,16,true,false).FramesPerSecond==15,"eco mode lowers animation cost");
        Check(RenderBudget.Select(32768,16,false,true).FramesPerSecond==0,"reduced motion disables recurring graph animation");
        Check(RenderBudget.Select(16384,6,false,false).PixelRatio<=1.25,"balanced devices cap canvas pixel density");
        string root=Path.Combine(Path.GetTempPath(),"astra-chat-delivery-"+Guid.NewGuid().ToString("N"));
        try
        {
            var chats=new AssistantChatStore(Path.Combine(root,"chats"));var chat=chats.Begin("Synthetic durable delivery","QA");chats.Append(chat.Id,new("user","Fixture request",DateTimeOffset.UtcNow));
            var jobs=new AgentJobStore(Path.Combine(root,"agents"));var job=jobs.Enqueue("Synthetic delivery fixture",["study"]);string complete=new string('λ',110000)+"OUTPUT_END";
            jobs.Update(job.Id,j=>{j.Status="Completed";j.CompletedUtc=DateTimeOffset.UtcNow;j.ConversationId=chat.Id;foreach(var step in j.Steps)step.Status="Completed";j.Steps.Last().Output=complete;});
            jobs.Conversations.Put(chat.Id,new(job.Id));
            Check(AgentConversationDelivery.DeliverCompleted(jobs,chats)==1&&chats.ReadAll(chat.Id).Last().Text==complete,"background delivery preserves the complete long Unicode response");
            Check(jobs.Conversations.Get(chat.Id)?.JobId==job.Id&&jobs.Records.Get(job.Id)!.ConversationDelivered,"conversation binding and completion survive a new store instance");
            chats.Append(chat.Id,new("user","Follow-up after delivery",DateTimeOffset.UtcNow));jobs.Update(job.Id,j=>j.ConversationDelivered=false);
            AgentConversationDelivery.DeliverCompleted(jobs,chats);
            Check(chats.ReadAll(chat.Id).Count==3,"a retry after message commit cannot duplicate the reply");
            Check(chats.ReadAll(chat.Id).Last().Text=="Follow-up after delivery","idempotent delivery preserves chronological follow-up history");
        }
        finally{if(Directory.Exists(root)){if(!Path.GetFullPath(root).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase))throw new IOException("Unsafe test cleanup");Directory.Delete(root,true);}}
        Console.WriteLine($"ALL {count} CONVERSATION AND RENDER POLICY CHECKS PASSED");
    }
}
