using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AstraOS.Core;
using AstraOS.Core.Agents;
using AstraOS.Core.SecondBrain;

internal static class AgentTeamTests
{
    public static async Task RunAsync()
    {
        int checks = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception("Agent team: " + name); checks++; Console.WriteLine("PASS Agent team: " + name); }
        Check(AgentCatalog.All.Length == 8 && AgentCatalog.All.Select(a => a.Id).Distinct().Count() == 8, "eight distinct role contracts");
        Check(AgentCatalog.All.All(a => a.Skills.Length >= 4 && a.Method.Length > 100), "each agent has precise skills and a method");
        Check(AgentCatalog.Route("Ricerca scientifica per progetto aerospaziale e GNC").Contains("aerospace"), "aerospace routing");
        Check(AgentCatalog.Route("Studia un paper e prepara un quiz").Contains("study"), "study routing");
        string root = Path.Combine(Path.GetTempPath(), "astra-agent-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var notes = new EncryptedNoteStore(Path.Combine(root, "notes"));
            var imported = Enumerable.Range(0, 1105).Select(i => new BrainNote(i.ToString(), "Note " + i, "Quaternion orbital evidence " + i, DateTimeOffset.UtcNow)).ToArray();
            notes.ImportArchive(JsonSerializer.Serialize(new { schemaVersion = 1, notes = imported }));
            Check(notes.Load().Count == 1105, "notebook exceeds the previous 1000-note quota");
            string legacyRoot = Path.Combine(root, "legacy"); Directory.CreateDirectory(legacyRoot);
            var original = new BrainNote("legacy-original", "Legacy", "Preserve original ciphertext and identity", DateTimeOffset.UtcNow);
            byte[] old = ProtectedData.Protect(JsonSerializer.SerializeToUtf8Bytes(new[] { original }), null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(Path.Combine(legacyRoot, "notes.dpapi"), old);
            var migrated = new EncryptedNoteStore(legacyRoot);
            Check(migrated.Load().Single().Id == original.Id && File.ReadAllBytes(Path.Combine(legacyRoot, "notes.dpapi")).SequenceEqual(old), "migration preserves identities and original encrypted archive");
            migrated.Delete(original.Id); Check(migrated.Load().Count == 0, "migration does not resurrect deleted notes");
            var store = new AgentJobStore(Path.Combine(root, "team"));
            var job = store.Enqueue("Compare orbital control", new[] { "aerospace", "engineering" });
            Check(job.Steps.Select(s => s.AgentId).SequenceEqual(new[] { "coordinator", "aerospace", "engineering", "reviewer", "coordinator" }), "specialist handoffs include planning, review and synthesis");
            Check(!Encoding.UTF8.GetString(File.ReadAllBytes(Directory.GetFiles(store.Records.DirectoryPath, "*.dpapi").Single())).Contains(job.Goal), "job objective encrypted at rest");
            int calls = 0;
            var engine = new AgentEngine(store, new FixtureTools(), (system, input, _) => { calls++; if(system.Contains("FINAL_EVIDENCE_AUDIT"))return Task.FromResult("{\"status\":\"PASS\",\"issues\":[]}"); return Task.FromResult("Supported calibration 95731 [note-fixture]. " + (calls == 2 ? "Unknown [web-invented]" : "")); });
            await engine.RunAsync(job.Id, default); job = store.Records.Get(job.Id)!;
            Check(job.Status == "Completed" && job.Steps.All(s => s.Status == "Completed") && calls == 6, "engine executes and checkpoints each specialist");
            Check(job.Steps[1].Output.Contains("UNVERIFIED source IDs: web-invented"), "unrecognized source citations are flagged");
            Check(job.Evidence.Count == 1, "source IDs deduplicate evidence across handoffs");
            var interrupted = store.Enqueue("resume", new[] { "study" }); int attempts = 0;
            var failing = new AgentEngine(store, new FixtureTools(), (_, _, _) => { if (++attempts == 2) throw new IOException("fixture interruption"); return Task.FromResult("Saved checkpoint"); });
            await failing.RunAsync(interrupted.Id, default);
            Check(store.Records.Get(interrupted.Id) is { Status: "Failed" } failed && failed.Steps[0].Status == "Completed", "failure preserves completed checkpoints");
            int resumed = 0;
            var resuming = new AgentEngine(store, new FixtureTools(), (system, _, _) => { resumed++; return Task.FromResult(system.Contains("FINAL_EVIDENCE_AUDIT")?"{\"status\":\"PASS\",\"issues\":[]}":"Resumed"); });
            store.Update(interrupted.Id, j => j.Status = "Queued"); await resuming.RunAsync(interrupted.Id, default);
            Check(resumed == 4 && store.Records.Get(interrupted.Id)!.Steps[0].Output == "Saved checkpoint", "resume skips completed steps");
            var transient=store.Enqueue("recover a transient connection interruption",new[]{"study"});store.Update(transient.Id,j=>{j.Status="Failed";j.Error="HTTP 503: temporarily unavailable";j.Steps[0].Status="Completed";j.Steps[0].Output="Checkpoint before retry";});
            var transientFailed=store.Records.Get(transient.Id)!;
            Check(AgentRecoveryPolicy.TryRecover(store,transientFailed)&&store.Records.Get(transient.Id) is {Status:"Queued",RepairAttempts:1} retry&&retry.Steps[0].Output=="Checkpoint before retry"&&new AgentLearning(store.Root).List().Any(l=>l.Category=="recovery"),"supervisor recognizes bounded transient recovery, keeps checkpoints, and records the procedure");
            await resuming.RunAsync(transient.Id,default);
            Check(store.Records.Get(transient.Id) is {Status:"Completed",RepairAttempts:1}&&store.Records.Get(transient.Id)!.Steps[0].Output=="Checkpoint before retry","recovered task completes without repeating its finished step");
            var denied=store.Enqueue("do not retry a persistent permission failure",new[]{"study"});store.Update(denied.Id,j=>{j.Status="Failed";j.Error="Access to the path is denied.";});
            Check(!AgentRecoveryPolicy.TryRecover(store,store.Records.Get(denied.Id)!)&&store.Records.Get(denied.Id) is {Status:"Failed",RepairAttempts:0},"automatic recovery leaves non-transient permission failures visible for review");
            var cancelled = store.Enqueue("cancel", new[] { "creator" }); store.Cancel(cancelled.Id); await engine.RunAsync(cancelled.Id, default);
            Check(store.Records.Get(cancelled.Id)!.Status == "Cancelled", "persistent cancellation prevents execution");
            var periodic = store.Enqueue("scheduled", new[] { "gaming" }, repeatMinutes: 1);
            periodic = store.Update(periodic.Id, j => j.Status = "Completed"); store.ScheduleNext(periodic); store.ScheduleNext(periodic);
            Check(store.Records.Enumerate().Count(j => j.SeriesId == periodic.SeriesId) == 2, "recurring scheduling is idempotent");
            store.Cancel(periodic.Id); Check(store.Records.Enumerate().Where(j => j.SeriesId == periodic.SeriesId).All(j => j.CancelRequested), "cancel stops the entire recurring series");
            var graph = BrainGraphBuilder.Build(new[] { original with { Tags = new[] { "GNC" } } }, Array.Empty<BrainDocument>(), new[] { job });
            Check(graph.Edges.All(e => graph.Nodes.Any(n => n.Id == e.Source) && graph.Nodes.Any(n => n.Id == e.Target)), "graph has no dangling edges");
            Check(graph.Edges.Any(e => e.Relation == "used source") && graph.Edges.Any(e => e.Relation == "has skill"), "graph traces work to evidence and precise agent skills");
            Check(AgentEngine.BuildInput(job, 4, 8192).Contains("95731"), "review handoffs preserve actual evidence");
            var simulationEvidence=new AgentEvidence("tool-simulation","Orbit reference","{\"checks\":{\"referenceCircularSpeedKmS\":7.668558175407055,\"referenceCircularPeriodSeconds\":5553.624271252227,\"maxRelativeEnergyDrift\":3.262138431870546E-11}}","SimulationLab:orbit","simulation");
            string verifiedFacts=AgentEngine.AppendToolFacts("Draft result.",new[]{simulationEvidence});
            Check(verifiedFacts.Contains("7.669 km/s")&&verifiedFacts.Contains("5554 s")&&verifiedFacts.Contains("[tool-simulation]")&&AgentEngine.AppendToolFacts(verifiedFacts,new[]{simulationEvidence})==verifiedFacts,"simulation facts are source-linked, correctly rounded, and idempotent");
            var numericalJob=store.Enqueue("Report numerical simulation findings",new[]{"aerospace"});bool auditSawFacts=false;
            var numericalEngine=new AgentEngine(store,new FixtureTools(simulationEvidence),(system,input,_)=>
            {
                if(system.Contains("FINAL_EVIDENCE_AUDIT")){auditSawFacts=input.Contains("7.669 km/s")&&input.Contains("5554 s")&&input.Contains("[tool-simulation]");return Task.FromResult("{\"status\":\"PASS\",\"issues\":[]}");}
                return Task.FromResult("Numerical findings from [tool-simulation].");
            });
            await numericalEngine.RunAsync(numericalJob.Id,default);var numericalReport=store.Records.Get(numericalJob.Id)!;
            Check(auditSawFacts&&!numericalReport.QualityNeedsReview&&numericalReport.Steps.Last().Output.Contains("7.669 km/s"),"final auditor receives deterministic source calculations before issuing a verdict");
            Check(Math.Abs(SemanticMemory.Cosine(new[]{1f,0f},new[]{2f,0f})-1)<1e-10&&SemanticMemory.Cosine(new[]{1f,0f},new[]{0f,1f})==0,"cosine ranking is normalized and distinguishes orthogonal embeddings");
            int embeddings=0;
            Task<float[]> Embed(string text,CancellationToken ct){embeddings++;return Task.FromResult(text.Contains("quaternion")||text.Contains("attitude")?new[]{1f,0f}:new[]{0f,1f});}
            var semantic=new SemanticMemory(Path.Combine(root,"vectors"));
            var corpus=new[]{new BrainNote("orbital","Control","quaternion dynamics",DateTimeOffset.UtcNow),new BrainNote("other","Creator","storyboard",DateTimeOffset.UtcNow)};
            var matches=await semantic.SearchAsync(corpus,"attitude","fixture-model",Embed,default);
            Check(matches.First().NoteId=="orbital","semantic retrieval can match different words through model vectors");
            await semantic.SearchAsync(corpus,"attitude","fixture-model",Embed,default);
            Check(embeddings==4,"encrypted vector cache avoids regenerating unchanged note embeddings");
            var skill=AgentSkillRules.FromMarkdown("SKILL","---\nname: numerical-review\ndescription: Check dimensions and sources\n---\nCheck units, cite actual source IDs and report missing evidence.");
            Check(skill.Name=="numerical-review"&&skill.Instructions.StartsWith("Check units"),"SKILL.md frontmatter imports as a reusable method");
            var vault=Path.Combine(root,"vault");Directory.CreateDirectory(vault);
            var vaultNotes=new[]{new BrainNote("one","Orbit","Links to [[Control]].",DateTimeOffset.UtcNow){Tags=new[]{"aerospace"}},new BrainNote("two","Control","Quaternion control.",DateTimeOffset.UtcNow)};
            Check(MarkdownVault.Export(vaultNotes,vault)==2,"shared memory exports to an Obsidian-ready Markdown vault");
            string exported=Path.Combine(vault,"ASTRA Second Brain",MarkdownVault.FileName(vaultNotes[0]));File.AppendAllText(exported,"\nUser edit to preserve.");
            MarkdownVault.Export(vaultNotes,vault);Check(File.ReadAllText(exported).Contains("User edit to preserve"),"vault export preserves user-edited Markdown files");
            var importedStore=new EncryptedNoteStore(Path.Combine(root,"vault-import"));var importedVault=MarkdownVault.Import(importedStore,vault);var again=MarkdownVault.Import(importedStore,vault);
            Check(importedVault.Imported>=2&&again.Imported==0,"Markdown vault import is idempotent without changing original files");
            Check(new AgentLearning(store.Root).List().Any(l=>l.Category=="citation"),"source audit failures become inspectable procedural lessons");
            Check(AgentEngine.AuditPasses("{\"status\":\"PASS\",\"issues\":[]}")&&!AgentEngine.AuditPasses("PASS")&&!AgentEngine.AuditPasses("{\"status\":\"PASS\",\"issues\":[\"invented measurement\"]}"),"final evidence audit fails closed on malformed or contradictory verdicts");
            var repairJob=store.Enqueue("Report only the calibration code",new[]{"study"});int audits=0;
            var repairing=new AgentEngine(store,new FixtureTools(),(system,_,_)=>Task.FromResult(system.Contains("FINAL_EVIDENCE_AUDIT")?(++audits==1?"{\"status\":\"NEEDS_REVIEW\",\"issues\":[\"Unsupported claim\"]}":"{\"status\":\"PASS\",\"issues\":[]}"):system.Contains("final editor")?"95731 [note-fixture]":"Draft with unsupported claim"));
            await repairing.RunAsync(repairJob.Id,default);var repaired=store.Records.Get(repairJob.Id)!;
            Check(audits==2&&!repaired.QualityNeedsReview&&repaired.Steps.Last().Output=="95731 [note-fixture]","final draft is corrected and re-audited against original evidence");
            new AgentLearning(store.Root).Observe("study","citation","learning-fixture");bool usedLesson=false;var learningJob=store.Enqueue("Use a reusable source-check method",new[]{"study"});
            var learner=new AgentEngine(store,new FixtureTools(),(system,input,_)=>{if(system.Contains("LEARNED VALIDATION METHODS"))usedLesson|=system.Contains("actual evidence ledger");return Task.FromResult(system.Contains("FINAL_EVIDENCE_AUDIT")?"{\"status\":\"PASS\",\"issues\":[]}":"Source-checked answer [note-fixture].");});
            await learner.RunAsync(learningJob.Id,default);
            Check(usedLesson&&new AgentLearning(store.Root).List().Any(l=>l.EvidenceJobId=="learning-fixture"),"procedural lessons persist and are supplied to future agent runs without changing model weights");
            ObsidianKnowledgeTests.Run(root, Check);
            FreeModelRouterTests.Run(Check);
            string longGoal=new string('g',110000)+"GOAL_TAIL_MARKER",longContext=new string('c',130000)+"CONTEXT_TAIL_MARKER",longHandoff=new string('h',150000)+"HANDOFF_TAIL_MARKER",longEvidence=new string('e',170000)+"SOURCE_TAIL_MARKER";
            var longJob=new AgentJob{Goal=longGoal,Context=longContext,Steps=[new AgentStep{AgentId="study",Status="Completed",Output=longHandoff},new AgentStep{AgentId="reviewer"}],Evidence=[new AgentEvidence("note-long-fixture","Long fixture",longEvidence,"fixture","memory")]};
            string completeInput=AgentEngine.BuildCompleteInput(longJob,1);
            Check(completeInput.Contains(longGoal,StringComparison.Ordinal)&&completeInput.Contains(longContext,StringComparison.Ordinal)&&completeInput.Contains(longHandoff,StringComparison.Ordinal)&&completeInput.Contains(JsonSerializer.Serialize(longEvidence),StringComparison.Ordinal)&&completeInput.EndsWith(longGoal,StringComparison.Ordinal),"cloud handoffs retain full long objectives, supplied context, prior output and source evidence through tail markers");
            var roleJob=store.Enqueue("Check role-aware model assignment",new[]{"study"});var roleCalls=new List<(string Role,string System)>();
            var roleEngine=new AgentEngine(store,new FixtureTools(),(role,system,input,ct)=>{roleCalls.Add((role,system));return Task.FromResult(system.Contains("FINAL_EVIDENCE_AUDIT")?"{\"status\":\"PASS\",\"issues\":[]}":"Supported result [note-fixture].");});
            await roleEngine.RunAsync(roleJob.Id,default);
            Check(roleCalls.Select(c=>c.Role).SequenceEqual(new[]{"coordinator","study","reviewer","coordinator","reviewer"})&&roleCalls.Where(c=>c.System.Contains("FINAL_EVIDENCE_AUDIT")).All(c=>c.Role=="reviewer"),"role-aware inference preserves specialist assignment and sends the final evidence audit to the reviewer model");
            Console.WriteLine($"ALL {checks} AGENT TEAM TESTS PASSED");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
    private sealed class FixtureTools(params AgentEvidence[] fixture) : IAgentTools
    {
        private readonly AgentEvidence[] _fixture=fixture.Length==0?new[]{new AgentEvidence("note-fixture", "Calibration", "Calibration code: 95731", "local fixture", "memory")}:fixture;
        public Task<IReadOnlyList<AgentEvidence>> GatherAsync(Specialist agent, AgentJob job, CancellationToken ct) => Task.FromResult<IReadOnlyList<AgentEvidence>>(_fixture);
    }
}
