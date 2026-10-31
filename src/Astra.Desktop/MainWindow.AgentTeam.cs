using System.Diagnostics;
using System.Text.Json;
using AstraOS.Core;
using AstraOS.Core.Agents;
using AstraOS.Core.SecondBrain;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Windows.Storage.Pickers;

namespace AstraOS.Desktop;

public sealed partial class MainWindow
{
    private readonly AgentJobStore _teamStore = new();
    private ListView? _teamJobs;
    private TextBox? _teamReport;
    private TextBlock? _teamStatus;
    private DispatcherTimer? _teamTimer;
    private WebView2? _brainGraphView;
    private string? _selectedTeamJob;
    private int _teamPage;
    private bool _refreshingTeam;
    private Func<Task>? _loadBrainGraph;
    private readonly List<string> _teamImages=new();
    private readonly AstraOS.Desktop.Ai.AssistantRecorder _teamRecorder=new();
    private System.Speech.Synthesis.SpeechSynthesizer? _teamSpeaker;

    private sealed record SimulationChoice(string Id, string Name);
    private static string TeamReport(AgentJob job) => "# " + job.Goal + "\n\nStatus: " + job.Status + " · " + job.CreatedUtc.ToString("O") + "\n" + job.Error + "\n\n" + string.Join("\n", job.Steps.Select(s => AgentCatalog.Get(s.AgentId).Name + ": " + s.Status)) + "\n\n" + job.Report + "\n\n## Evidence ledger\n\n" + string.Join("\n\n", job.Evidence.Select(e => $"[{e.Id}] {e.Title}\n{e.Location}\n{e.Text}"));
    private void RefreshTeamReport()
    {
        if (_selectedTeamJob is not null && _teamStore.Records.Get(_selectedTeamJob) is { } job && _teamReport is not null)
        {
            string report = TeamReport(job); if (_teamReport.Text != report) _teamReport.Text = report;
            int completed = job.Steps.Count(s => s.Status == "Completed");
            if (_teamProgress is not null) { _teamProgress.IsIndeterminate = job.Status == "Running" && completed == 0; _teamProgress.Value = job.Steps.Count == 0 ? 0 : 100.0 * completed / job.Steps.Count; }
            string stage = job.Steps.FirstOrDefault(s => s.Status != "Completed")?.AgentId ?? "coordinator";
            if (_teamStepStatus is not null) _teamStepStatus.Text = job.Status + " · " + completed + "/" + job.Steps.Count + " handoffs complete" + (job.Status == "Running" ? " · " + AgentCatalog.Get(stage).Name : "") + (job.QualityNeedsReview ? " · Evidence needs review" : "") + (job.Error.Length > 0 ? "\n" + job.Error : "");
        }
    }
    private void RefreshTeamJobs()
    {
        if (_teamJobs is null) return;
        _refreshingTeam = true;
        try
        {
            var all = _teamStore.Records.Enumerate().OrderByDescending(j => j.CreatedUtc).ToArray();
            var jobs = all.Skip(_teamPage * 30).Take(30).ToArray();
            string fingerprint = _teamPage + ":" + string.Join("|", jobs.Select(j => j.Id + ":" + j.Status + ":" + j.Steps.Count(s => s.Status == "Completed") + ":" + j.QualityNeedsReview + ":" + j.Error));
            if (_teamJobsFingerprint != fingerprint)
            {
                _teamJobs.Items.Clear();
                foreach (var job in jobs)
                {
                    var row = new StackPanel { Spacing = 4, Margin = new Thickness(2, 7, 2, 7) };
                    var title = Label(job.Goal.Replace('\n', ' '), 14); title.MaxLines = 2; title.TextTrimming = TextTrimming.CharacterEllipsis; title.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold; row.Children.Add(title);
                    row.Children.Add(Label(job.Status + " · " + job.Steps.Count(s => s.Status == "Completed") + "/" + job.Steps.Count + " handoffs · " + job.CreatedUtc.ToLocalTime().ToString("dd MMM HH:mm") + (job.QualityNeedsReview ? " · Needs evidence review" : ""), 11));
                    var item = new ListViewItem { Tag = job.Id, Content = row, HorizontalContentAlignment = HorizontalAlignment.Stretch };
                    _teamJobs.Items.Add(item); if (job.Id == _selectedTeamJob) _teamJobs.SelectedItem = item;
                }
                _teamJobsFingerprint = fingerprint;
            }
            if (_teamJobCount is not null) _teamJobCount.Text = all.Length + " jobs · " + all.Count(j => j.Status is "Running" or "Queued") + " active · " + all.Count(j => j.Status == "Completed") + " complete";
            if (_teamEmpty is not null) _teamEmpty.Visibility = jobs.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
            RefreshTeamReport();
        }
        finally { _refreshingTeam = false; }
    }
    private void StartTeamWorker()
    {
        try
        {
            if (!_teamStore.Configuration.Enabled) { _teamStatus!.Text = "Enable the background worker first."; return; }
            string executable = Path.Combine(AppContext.BaseDirectory, "Astra.AgentSupervisor.exe");
            if (!File.Exists(executable)) { _teamStatus!.Text = "Agent worker missing. Use the complete distribution folder."; return; }
            using var process = Process.Start(new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = AppContext.BaseDirectory });
        }
        catch (Exception ex) { if (_teamStatus is not null) _teamStatus.Text = ex.Message; }
    }

    private UIElement BuildBrainGraph()
    {
        var panel = new StackPanel { Spacing = 10 };
        panel.Children.Add(Label("Knowledge graph · shared memory", 23));
        panel.Children.Add(Label("Explore notes, documents, agents, skills, projects and job sources. Edges describe recorded relationships; they do not assert that an AI claim is true. Search, filter, pan, zoom and select a node to inspect it.", 13));
        var host = new Grid { Height = 620 };
        var status = Label("Open the graph to load the local interactive view.", 12); panel.Children.Add(status);
        _loadBrainGraph = async () =>
        {
            try
            {
                if (_brainGraphView is null)
                {
                    var view = new WebView2(); host.Children.Add(view); _brainGraphView = view;
                    var env = await CoreWebView2Environment.CreateWithOptionsAsync(null, Path.Combine(AstraPaths.UserDataRoot, "WebView2"), null);
                    await view.EnsureCoreWebView2Async(env);
                    var core = view.CoreWebView2; core.Settings.AreHostObjectsAllowed = false; core.Settings.AreDevToolsEnabled = false;
                    core.SetVirtualHostNameToFolderMapping("astra-brain.local", Path.Combine(AppContext.BaseDirectory, "BrainGraph"), CoreWebView2HostResourceAccessKind.DenyCors);
                    core.NavigationStarting += (_, e) => { if (e.Uri != "https://astra-brain.local/index.html") e.Cancel = true; };
                    core.NewWindowRequested += (_, e) => e.Handled = true;
                    core.WebMessageReceived += async (_, e) => { if (e.Source == "https://astra-brain.local/index.html" && e.TryGetWebMessageAsString() == "ready") await SendBrainGraphAsync(); };
                    core.Navigate("https://astra-brain.local/index.html");
                }
                else await SendBrainGraphAsync();
                status.Text = "Local graph · use the filters to focus on a project or topic.";
            }
            catch (Exception ex) { status.Text = ex.Message; }
        };
        panel.Children.Add(Action("Open / refresh graph", async (_, _) => await _loadBrainGraph()));
        panel.Children.Add(host);
        Closed += (_, _) => _brainGraphView?.Close(); return panel;
    }
    private async Task SendBrainGraphAsync()
    {
        var graph = await Task.Run(() => { var config=_teamStore.Configuration;IReadOnlyList<BrainNote> vault=string.IsNullOrWhiteSpace(config.VaultPath)?Array.Empty<BrainNote>():ObsidianKnowledge.GraphNotes(config.VaultPath,_windowLifetime.Token);return BrainGraphBuilder.Build(_notes.Enumerate().Concat(vault), _documents.List(), _teamStore.Records.Enumerate()); });
        _brainGraphView?.CoreWebView2?.PostWebMessageAsJson(JsonSerializer.Serialize(new { nodes = graph.Nodes.Select(n => new { id = n.Id, label = n.Label, kind = n.Kind, detail = n.Detail }), edges = graph.Edges.Select(e => new { source = e.Source, target = e.Target, relation = e.Relation }) }));
    }

    private async Task RunAgentBrainSmokeAsync()
    {
        var checks=new List<object>();bool passed=true;
        void Check(string name,bool ok){checks.Add(new{name,passed=ok});passed&=ok;}
        try
        {
            await Task.Delay(1000);Navigate("brain");
            var tabs=BrainPanel.Children.OfType<TabView>().Single();
            Check("Native specialist team and graph tabs exist",tabs.TabItems.OfType<TabViewItem>().Any(t=>t.Header.ToString()=="Agent team")&&tabs.TabItems.OfType<TabViewItem>().Any(t=>t.Header.ToString()=="Knowledge graph"));
            _notes.Add("Aerospace calibration","The ASTRA calibration code is 95731. A quaternion represents attitude.",new[]{"GNC","aerospace"});
            byte[] speech=await AstraOS.Desktop.Ai.AssistantMedia.SynthesizeAsync("Astra assistant supports voice input and voice output.",_windowLifetime.Token);
            Check("Voice output produces actual WAV audio",speech.Length>1000&&System.Text.Encoding.ASCII.GetString(speech,0,4)=="RIFF");
            string voiceFile=Path.Combine(AstraPaths.UserDataRoot,"voice-calibration.wav");await File.WriteAllBytesAsync(voiceFile,speech);
            var transcript=await AstraOS.Desktop.Ai.AssistantMedia.TranscribeAsync(voiceFile,_windowLifetime.Token);
            Check("Configured online audio model transcribes actual speech",transcript.Text.Length>10);
            var qaCompute=_teamStore.Configuration;qaCompute.ContextSize=1_000_000;qaCompute.OutputTokens=0;qaCompute.SemanticSearch=false;qaCompute.PreferFreeCloud=true;qaCompute.FallbackToGpu=false;qaCompute.UseGpu=false;_teamStore.Settings.Put("runtime",qaCompute);
            var job=_teamStore.Enqueue("Read the calibration source and report the ASTRA calibration code 95731. Keep each handoff brief.",new[]{"study"});
            string fixture=Path.Combine(AstraPaths.UserDataRoot,"vision-fixture.png");
            if(File.Exists(fixture))
            {
                byte[] pixels=await File.ReadAllBytesAsync(fixture);string imageId=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(pixels));_teamStore.Images.Put(imageId,new("Red circle vision fixture",pixels));_teamStore.Update(job.Id,j=>j.ImageIds.Add(imageId));
            }
            _selectedTeamJob=job.Id;RefreshTeamJobs();StartTeamWorker();
            bool interrupted=false,restarted=false;int oldWorker=0;string checkpoint="";
            for(int i=0;i<900;i++)
            {
                await Task.Delay(1000);job=_teamStore.Records.Get(job.Id)!;
                string supervisorFile=Path.Combine(_teamStore.Root,"supervisor.json");
                if(File.Exists(supervisorFile))
                {
                    try
                    {
                        using var info=JsonDocument.Parse(await File.ReadAllTextAsync(supervisorFile));int pid=info.RootElement.GetProperty("workerPid").GetInt32();
                        if(!interrupted&&job.Steps[0].Status=="Completed"&&job.Status=="Running")
                        {
                            using var process=System.Diagnostics.Process.GetProcessById(pid);
                            if(process.MainModule?.FileName==Path.Combine(AppContext.BaseDirectory,"Astra.AgentWorker.exe"))
                            {checkpoint=job.Steps[0].Output;oldWorker=pid;process.Kill(true);interrupted=true;}
                        }
                        else if(interrupted&&pid!=oldWorker)restarted=true;
                    }
                    catch(Exception ex) when(ex is IOException or ArgumentException or System.ComponentModel.Win32Exception or JsonException){}
                }
                if(job.Status is "Completed" or "Failed" or "Cancelled")break;
            }
            Check("Supervisor restarts a deliberately interrupted worker",interrupted&&restarted);
            Check("Completed specialist checkpoint survives process death",checkpoint.Length>0&&job.Steps[0].Output==checkpoint);
            Check("Configured online model completes planning, specialist, review and synthesis",job.Status=="Completed"&&job.Steps.All(s=>s.Status=="Completed"));
            Check("Memory retrieval provides actual calibration evidence",job.Evidence.Any(e=>e.Text.Contains("95731")));
            Check("Final synthesis retains the actual calibration code",job.Steps.Last().Output.Contains("95731"));
            if(File.Exists(fixture))Check("Configured online pixel vision recognizes the red test shape",job.Evidence.Any(e=>e.Kind=="vision"&&e.Text.Contains("red",StringComparison.OrdinalIgnoreCase)));
            Check("One ASTRA Assistant entry across profiles",AssistantName=="ASTRA Assistant"&&BrainNav.Visibility==Visibility.Collapsed);
            if(job.Status=="Failed")checks.Add(new{name="Worker failure detail",detail=job.Error});
            tabs.SelectedItem=tabs.TabItems.OfType<TabViewItem>().Single(t=>t.Header.ToString()=="Knowledge graph");
            await _loadBrainGraph!();
            for(int i=0;i<30;i++){await Task.Delay(300);if(await _brainGraphView!.CoreWebView2.ExecuteScriptAsync("Boolean(window.astraGraph && window.astraGraph.state.nodes > 0)")=="true")break;}
            Check("Embedded graph renders actual memory and job nodes",await _brainGraphView!.CoreWebView2.ExecuteScriptAsync("window.astraGraph.state.nodes > 20 && window.astraGraph.state.edges > 10")=="true");
            string noteId="note-"+_notes.Load().First(n=>n.Title=="Aerospace calibration").Id;
            await _brainGraphView.CoreWebView2.ExecuteScriptAsync("window.astraGraph.inspect("+JsonSerializer.Serialize(noteId)+")");
            Check("Selecting a graph node displays original source content",await _brainGraphView.CoreWebView2.ExecuteScriptAsync("document.getElementById('detail').textContent.includes('95731')")=="true");
            await _brainGraphView.CoreWebView2.ExecuteScriptAsync("document.getElementById('search').value='Aerospace calibration';document.getElementById('search').dispatchEvent(new Event('input'))");
            Check("Graph search filters real nodes",await _brainGraphView.CoreWebView2.ExecuteScriptAsync("window.astraGraph.state.visible > 0 && window.astraGraph.state.visible < window.astraGraph.state.nodes")=="true");
            await _brainGraphView.CoreWebView2.ExecuteScriptAsync("document.getElementById('search').value='';document.getElementById('search').dispatchEvent(new Event('input'))");
            _brainGraphView.StartBringIntoView();
        }
        catch(Exception ex){passed=false;checks.Add(new{name="Native QA exception",detail=ex.ToString()});}
        await File.WriteAllTextAsync(Path.Combine(AstraPaths.UserDataRoot,"agent-brain-native.json"),JsonSerializer.Serialize(new{passed,modelTested=true,checks},new JsonSerializerOptions{WriteIndented=true}));
    }
}
