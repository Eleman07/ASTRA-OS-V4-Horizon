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
        Check(AgentCatalog.All.Length == 9 && AgentCatalog.All.Select(a => a.Id).Distinct().Count() == 9, "nine distinct role contracts including everyday/system assistance");
        Check(AgentCatalog.All.All(a => a.Skills.Length >= 4 && a.Method.Length > 100), "each agent has precise skills and a method");
        Check(AgentCatalog.Route("Ricerca scientifica per progetto aerospaziale e GNC").Contains("aerospace"), "aerospace routing");
        Check(AgentCatalog.Route("Studia un paper e prepara un quiz").Contains("study"), "study routing");
        string root = Path.Combine(Path.GetTempPath(), "astra-agent-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var freshStore = new AgentJobStore(Path.Combine(root, "fresh-provider"));
            Check(freshStore.Configuration.UseCodex && freshStore.Configuration.PreferFreeCloud && !freshStore.Configuration.UseGpu, "new profiles choose Codex online without a local GPU model");
            freshStore.Settings.Put("runtime", new AgentRuntimeSettings { UseCodex = false, PreferFreeCloud = true, UseGpu = false });
            Check(!freshStore.Configuration.UseCodex && freshStore.Configuration.PreferFreeCloud, "an explicitly saved OpenRouter choice is preserved on profile load");
            string unicode="L'orbita è già definita: Δv = 2π, θ = 45°, accelerazione μ/r². 🚀🛰️\n\"citazione\" e percorso C:\\Progetti\\orbita\r\n\tcontrollo:\u0001\u0000";
            string quotedUnicode=PromptData.Quote(unicode);
            Check(JsonSerializer.Deserialize<string>(quotedUnicode)==unicode,"prompt JSON quoting roundtrips Italian, Greek, emoji, quotes, backslashes and control characters exactly");
            Check(quotedUnicode.Contains("è già",StringComparison.Ordinal)&&quotedUnicode.Contains("Δv = 2π",StringComparison.Ordinal)&&!quotedUnicode.Contains('\n')&&quotedUnicode.StartsWith('"')&&quotedUnicode.EndsWith('"'),"ordinary non-ASCII evidence stays readable inside a valid JSON string while line breaks remain escaped");
            Check(quotedUnicode.Length<JsonSerializer.Serialize(unicode).Length,"Unicode-preserving quoting avoids the default escape expansion without dropping source characters");
            string longUnicode=string.Concat(Enumerable.Repeat(unicode,2200))+"UNICODE_SOURCE_TAIL";
            Check(JsonSerializer.Deserialize<string>(PromptData.Quote(longUnicode))==longUnicode,"complete long Unicode evidence roundtrips without an application character cap");
            var unicodeJob=new AgentJob{Goal="Conserva l'obiettivo Δv",Context="Contesto integrale: è già noto",Steps=[new(){AgentId="coordinator"}],Evidence=[new("note-unicode","Unicode evidence",longUnicode,"fixture","memory")]};
            string unicodePrompt=AgentEngine.BuildCompleteInput(unicodeJob,0),sourceHeader="SOURCE [note-unicode] Unicode evidence | fixture (quoted evidence):\n";
            int quoteStart=unicodePrompt.IndexOf(sourceHeader,StringComparison.Ordinal)+sourceHeader.Length,quoteEnd=unicodePrompt.IndexOf('\n',quoteStart);
            Check(JsonSerializer.Deserialize<string>(unicodePrompt[quoteStart..quoteEnd])==longUnicode&&unicodePrompt.Contains(unicodeJob.Goal,StringComparison.Ordinal)&&unicodePrompt.Contains(unicodeJob.Context,StringComparison.Ordinal),"the actual handoff retains the full decoded Unicode source plus the unchanged objective and context");
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
            await AgentProviderRetryTests.RunAsync(root, Check);
            await AgentArtifactExporterTests.RunAsync(root, Check);
            SimulationEvidenceArchiveTests.Run(root, Check);
            FreeModelRouterTests.Run(Check);
            string longGoal=new string('g',110000)+"GOAL_TAIL_MARKER",longContext=new string('c',130000)+"CONTEXT_TAIL_MARKER",longHandoff=new string('h',150000)+"HANDOFF_TAIL_MARKER",longEvidence=new string('e',170000)+"SOURCE_TAIL_MARKER";
            var longJob=new AgentJob{Goal=longGoal,Context=longContext,Steps=[new AgentStep{AgentId="study",Status="Completed",Output=longHandoff},new AgentStep{AgentId="reviewer"}],Evidence=[new AgentEvidence("note-long-fixture","Long fixture",longEvidence,"fixture","memory")]};
            string completeInput=AgentEngine.BuildCompleteInput(longJob,1);
            Check(completeInput.Contains(longGoal,StringComparison.Ordinal)&&completeInput.Contains(longContext,StringComparison.Ordinal)&&completeInput.Contains(longHandoff,StringComparison.Ordinal)&&completeInput.Contains(JsonSerializer.Serialize(longEvidence),StringComparison.Ordinal)&&completeInput.EndsWith(longGoal,StringComparison.Ordinal),"cloud handoffs retain full long objectives, supplied context, prior output and source evidence through tail markers");
            Check(AgentEngine.BuildInput(longJob,1,512)==completeInput,"local compatibility handoffs preserve the complete input even when the selected model context is smaller; runtime must report overflow explicitly");
            var localLongStore=new AgentJobStore(Path.Combine(root,"local-full-context"));
            localLongStore.Settings.Put("runtime",new AgentRuntimeSettings{PreferFreeCloud=false,UseGpu=true,ContextSize=512,AutoLearn=false});
            var localLongJob=localLongStore.Enqueue(longGoal,["study"],longContext);bool localInputFull=true,localAuditFull=false;
            string candidate=new string('d',180000)+"CANDIDATE_TAIL_MARKER [note-long-fixture]";
            var localLongEngine=new AgentEngine(localLongStore,new FixtureTools(new AgentEvidence("note-long-fixture","Long fixture",longEvidence,"fixture","memory")),(system,input,ct)=>
            {
                if(system.Contains("FINAL_EVIDENCE_AUDIT",StringComparison.Ordinal)){localAuditFull=input.Contains(longGoal,StringComparison.Ordinal)&&input.Contains(longContext,StringComparison.Ordinal)&&input.Contains(longEvidence,StringComparison.Ordinal)&&input.Contains(candidate,StringComparison.Ordinal);return Task.FromResult("{\"status\":\"PASS\",\"issues\":[]}");}
                localInputFull&=input.Contains(longGoal,StringComparison.Ordinal)&&input.Contains(longContext,StringComparison.Ordinal)&&input.Contains(JsonSerializer.Serialize(longEvidence),StringComparison.Ordinal);
                return Task.FromResult(candidate);
            });
            await localLongEngine.RunAsync(localLongJob.Id,default);
            Check(localInputFull&&localAuditFull&&localLongStore.Records.Get(localLongJob.Id) is {Status:"Completed"} localLongResult&&localLongResult.Steps.Last().Output==candidate,"local agent transport and final evidence audit retain full long source and candidate tails without claiming an actual model test");
            var roleJob=store.Enqueue("Check role-aware model assignment",new[]{"study"});var roleCalls=new List<(string Role,string System)>();
            var roleEngine=new AgentEngine(store,new FixtureTools(),(role,system,input,ct)=>{roleCalls.Add((role,system));return Task.FromResult(system.Contains("FINAL_EVIDENCE_AUDIT")?"{\"status\":\"PASS\",\"issues\":[]}":"Supported result [note-fixture].");});
            await roleEngine.RunAsync(roleJob.Id,default);
            Check(roleCalls.Select(c=>c.Role).SequenceEqual(new[]{"coordinator","study","reviewer","coordinator","reviewer"})&&roleCalls.Where(c=>c.System.Contains("FINAL_EVIDENCE_AUDIT")).All(c=>c.Role=="reviewer"),"role-aware inference preserves specialist assignment and sends the final evidence audit to the reviewer model");
            var stageStore=new AgentJobStore(Path.Combine(root,"stage-contracts"));stageStore.Settings.Put("runtime",new AgentRuntimeSettings{AutoLearn=false});
            var stageJob=stageStore.Enqueue("Produce a complete Python software project with source files, tests and README, retaining original source identifiers.",["aerospace","engineering"]);
            var stages=new List<(string Role,string Instruction,string Input)>();int coordinators=0;
            const string contract="Execution contract R1: complete Python files; R2: tests; R3: README. Aerospace owns equations, engineering owns code, reviewer checks evidence. No computation has been executed.";
            const string engineeringFiles="```file:src/orbit.py\n# Proposed numerical component Δv; not executed.\nREFERENCE_LABEL = 'fixture'\n```\n";
            const string finalFiles=engineeringFiles+"```file:README.md\n# Proposed project\nCode and tests require execution; this is a deterministic reporting fixture.\n```\n";
            var staged=new AgentEngine(stageStore,new FixtureTools(),(role,instruction,input,ct)=>
            {
                stages.Add((role,instruction,input));
                if(instruction.Contains("FINAL_EVIDENCE_AUDIT",StringComparison.Ordinal))return Task.FromResult("{\"status\":\"PASS\",\"issues\":[]}");
                return Task.FromResult(role switch{"coordinator"=>++coordinators==1?contract:finalFiles,"engineering"=>engineeringFiles,"aerospace"=>"Aerospace contribution: state frame, units and two-body assumptions. [note-fixture]",_=>"Review contribution: verify requirements and preserve unexecuted status. [note-fixture]"});
            });
            await staged.RunAsync(stageJob.Id,default);
            var handoffs=stages.Where(stage=>!stage.Instruction.Contains("FINAL_EVIDENCE_AUDIT",StringComparison.Ordinal)).ToArray();
            Check(handoffs[0].Role=="coordinator"&&handoffs[0].Instruction.Contains("execution contract only",StringComparison.Ordinal)&&handoffs[0].Instruction.Contains("Do not write source files",StringComparison.Ordinal)&&handoffs[0].Instruction.Contains(AgentCatalog.Get("engineering").Name,StringComparison.Ordinal)&&!handoffs[0].Instruction.Contains("file:relative/path.py",StringComparison.Ordinal),"the initial coordinator contract delegates implementation to the selected team and does not receive the final file-generation instruction");
            Check(handoffs.Skip(1).SkipLast(1).All(stage=>stage.Instruction.Contains("own skills and assigned requirements",StringComparison.Ordinal))&&handoffs.Single(stage=>stage.Role=="engineering").Input.Contains(contract,StringComparison.Ordinal),"specialist handoffs receive the original execution contract and a scoped concrete contribution instead of another whole-project request");
            Check(handoffs.Last().Role=="coordinator"&&handoffs.Last().Instruction.Contains("file:relative/path.py",StringComparison.Ordinal)&&!handoffs.Last().Instruction.Contains("INITIAL EXECUTION CONTRACT",StringComparison.Ordinal)&&handoffs.Last().Input.Contains(engineeringFiles,StringComparison.Ordinal)&&stageStore.Records.Get(stageJob.Id)!.Steps.Last().Output==finalFiles,"final synthesis still receives complete engineering files and the concrete full-file contract rather than being limited to planning");
            var stageArtifacts=AgentArtifactExporter.Export(Path.Combine(root,"staged-project"),stageStore.Records.Get(stageJob.Id)!)!;
            Check(stageArtifacts.Files.Count==2&&File.ReadAllText(Path.Combine(stageArtifacts.Directory,"src","orbit.py")).Contains("component Δv",StringComparison.Ordinal),"distinct planning, specialist and final stages still export complete Unicode source files in a deterministic inference fixture");
            const string projectGoal="Create a complete Python software project. Deliver source files, README and tests.";
            Check(AgentArtifactExporter.RequireFileArtifacts(projectGoal)&&AgentArtifactExporter.RequireFileArtifacts("Realizza un progetto software di simulazione Python. Consegna i file completi.")&&AgentArtifactExporter.IsSoftwareRequest("Explain a Python code example")&&!AgentArtifactExporter.RequireFileArtifacts("Explain a Python code example")&&!AgentArtifactExporter.RequireFileArtifacts("How do I create a Python project with files?")&&!AgentArtifactExporter.RequireFileArtifacts("Spiegami il file Python di un progetto aerospaziale"),"concrete project delivery requires files while conceptual coding questions and tutoring do not");
            int filesBefore=Directory.GetFiles(root,"*",SearchOption.AllDirectories).Length;
            Check(AgentArtifactExporter.ProposedFileCount(finalFiles)==2&&AgentArtifactExporter.ProposedFileCount("```python\nprint('ordinary code')\n```\n")==0&&Directory.GetFiles(root,"*",SearchOption.AllDirectories).Length==filesBefore,"public proposal validation uses explicit file fences without writing files or directories");
            var formatStore=new AgentJobStore(Path.Combine(root,"model-format-repair"));formatStore.Settings.Put("runtime",new AgentRuntimeSettings{AutoLearn=false});
            var formatJob=formatStore.Enqueue(projectGoal,["engineering"]);int formatRepairs=0;bool fullRepairEvidence=false;
            const string noFiles="Complete proposed source:\n```python\nprint('not an exported file')\n```\n";
            var formatEngine=new AgentEngine(formatStore,new FixtureTools(new AgentEvidence("note-format","Original Unicode evidence",longUnicode,"fixture","memory")),(role,instruction,input,ct)=>
            {
                if(instruction.Contains("format repair agent",StringComparison.Ordinal))
                {formatRepairs++;fullRepairEvidence=input.Contains(noFiles,StringComparison.Ordinal)&&input.Contains(longUnicode,StringComparison.Ordinal)&&input.Contains("No explicit exportable file proposals",StringComparison.Ordinal);return Task.FromResult(finalFiles);}
                return Task.FromResult(instruction.Contains("FINAL_EVIDENCE_AUDIT",StringComparison.Ordinal)?"{\"status\":\"PASS\",\"issues\":[]}":noFiles);
            });
            await formatEngine.RunAsync(formatJob.Id,default);var formatFixed=formatStore.Records.Get(formatJob.Id)!;
            Check(formatRepairs==1&&fullRepairEvidence&&formatFixed.Status=="Completed"&&AgentArtifactExporter.ProposedFileCount(formatFixed.Steps.Last().Output)==2,"a missing-file deliverable is corrected once by inference using the actual validation error, full candidate and complete original evidence");
            foreach(var invalid in new[]{("unclosed","```file:src/bad.py\nUNFINISHED_CANDIDATE"),("traversal","```file:../escape.py\nUNSAFE_CANDIDATE\n```\n")})
            {
                var invalidStore=new AgentJobStore(Path.Combine(root,"format-invalid-"+invalid.Item1));invalidStore.Settings.Put("runtime",new AgentRuntimeSettings{AutoLearn=false});
                var invalidJob=invalidStore.Enqueue(projectGoal,["engineering"]);int repairs=0,invalidAudits=0;
                var invalidEngine=new AgentEngine(invalidStore,new FixtureTools(),(role,instruction,input,ct)=>
                {
                    if(instruction.Contains("format repair agent",StringComparison.Ordinal)){repairs++;return Task.FromResult(invalid.Item2);}
                    if(instruction.Contains("FINAL_EVIDENCE_AUDIT",StringComparison.Ordinal))invalidAudits++;
                    return Task.FromResult(noFiles);
                });
                await invalidEngine.RunAsync(invalidJob.Id,default);var invalidResult=invalidStore.Records.Get(invalidJob.Id)!;
                Check(invalidResult.Status=="Failed"&&repairs==1&&invalidAudits==0&&invalidResult.Steps.Last().Output==invalid.Item2&&invalidResult.Steps.Last().Status!="Completed"&&invalidResult.Steps.Take(3).All(step=>step.Status=="Completed")&&invalidResult.ArtifactDirectory=="","unrepaired "+invalid.Item1+" proposals fail before the audit, retain full candidate/checkpoints and never claim completed files");
            }
            var conceptualStore=new AgentJobStore(Path.Combine(root,"conceptual-code"));conceptualStore.Settings.Put("runtime",new AgentRuntimeSettings{AutoLearn=false});
            var conceptual=conceptualStore.Enqueue("Explain how a Python orbital simulator works, without creating files.",["study"]);int conceptualRepairs=0;
            var conceptualEngine=new AgentEngine(conceptualStore,new FixtureTools(),(role,instruction,input,ct)=>{if(instruction.Contains("format repair agent",StringComparison.Ordinal))conceptualRepairs++;return Task.FromResult(instruction.Contains("FINAL_EVIDENCE_AUDIT",StringComparison.Ordinal)?"{\"status\":\"PASS\",\"issues\":[]}":"Conceptual explanation with no proposed files.");});
            await conceptualEngine.RunAsync(conceptual.Id,default);
            Check(conceptualStore.Records.Get(conceptual.Id)!.Status=="Completed"&&conceptualRepairs==0,"conceptual tutoring can complete without file proposals or an unnecessary format-repair call");
            var auditFormatStore=new AgentJobStore(Path.Combine(root,"audit-file-preservation"));auditFormatStore.Settings.Put("runtime",new AgentRuntimeSettings{AutoLearn=false});
            var auditFormatJob=auditFormatStore.Enqueue(projectGoal,["engineering"]);int fileAudits=0;
            var auditFormatEngine=new AgentEngine(auditFormatStore,new FixtureTools(),(role,instruction,input,ct)=>Task.FromResult(instruction.Contains("FINAL_EVIDENCE_AUDIT",StringComparison.Ordinal)?++fileAudits==1?"{\"status\":\"NEEDS_REVIEW\",\"issues\":[\"Remove unsupported claim\"]}":"{\"status\":\"PASS\",\"issues\":[]}":instruction.Contains("final editor",StringComparison.Ordinal)?"Editor removed all file proposals.":finalFiles));
            await auditFormatEngine.RunAsync(auditFormatJob.Id,default);var removedFiles=auditFormatStore.Records.Get(auditFormatJob.Id)!;
            Check(removedFiles.Status=="Failed"&&removedFiles.Steps.Last().Output=="Editor removed all file proposals.","a later audit correction cannot turn an exportable project into prose and still mark the final delivery completed");
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
