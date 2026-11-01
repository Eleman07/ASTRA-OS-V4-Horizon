namespace AstraOS.Core.Agents;

public sealed record AgentLesson(string AgentId,string Category,string Method,string EvidenceJobId,DateTimeOffset UpdatedUtc);
/// <summary>Inspectable procedural learning from recognized failures; does not train model weights.</summary>
public sealed class AgentLearning
{
    private readonly EncryptedRecords<AgentLesson> _records;
    public AgentLearning(string root)=>_records=new(Path.Combine(root,"lessons"));
    public IEnumerable<AgentLesson> List()=>_records.Enumerate();
    public string Instructions(string role)=>string.Join("\n",List().Where(l=>l.AgentId==role).Select(l=>l.Method));
    public void Observe(string role,string category,string job)
    {
        string method=category switch
        {
            "citation"=>"Check every bracketed source ID against the actual evidence ledger before returning the answer. Remove unsupported citations and explicitly identify missing evidence.",
            "numeric"=>"Use deterministic tool records for numerical facts. Preserve units, reference frame and requested rounding. Separate a model assumption from an observation.",
            "recovery"=>"After a transient failure, resume from the last completed checkpoint. Report the actual tool outcome and preserve unresolved inputs; do not pretend a failed action succeeded.",
            _=>throw new ArgumentException("Unknown learning category.")
        };
        using var guard=_records.Lock();_records.Put(role+":"+category,new(role,category,method,job,DateTimeOffset.UtcNow));
    }
}
