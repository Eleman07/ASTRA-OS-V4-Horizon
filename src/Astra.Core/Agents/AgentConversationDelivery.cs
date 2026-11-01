namespace AstraOS.Core.Agents;

/// <summary>Durable, idempotent delivery independent of whether the chat window is open.</summary>
public static class AgentConversationDelivery
{
    public static string Output(AgentJob job) => job.Steps.Last().Output
        +(job.ArtifactDirectory.Length>0?"\n\nArtefatti concreti: "+job.ArtifactDirectory:"")
        +(job.QualityNeedsReview?"\n\nEvidenze ancora da verificare: consulta il report e le fonti.":"");
    public static int DeliverCompleted(AgentJobStore store,AssistantChatStore? chats=null)
    {
        chats??=new();int count=0;
        foreach(var job in store.Records.Enumerate().Where(j=>j.Status=="Completed"&&j.ConversationId.Length>0&&!j.ConversationDelivered))
        {
            try
            {
                chats.AppendDeliveryOnce(job.ConversationId,job.Id,new("assistant",Output(job),job.CompletedUtc??DateTimeOffset.UtcNow));
                store.Update(job.Id,j=>j.ConversationDelivered=true);count++;
            }
            catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or InvalidDataException)
            {store.Update(job.Id,j=>j.Error="Conversation delivery pending: "+ex.Message);}
        }
        return count;
    }
}
