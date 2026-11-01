using System.Text.Json;
using AstraOS.Core;
using AstraOS.Core.Agents;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace AstraOS.Desktop;
public sealed partial class MainWindow
{
    private async Task RunUnifiedUiSmokeAsync()
    {
        var checks=new List<object>();bool passed=true;
        void Check(string name,bool value){checks.Add(new{name,passed=value});passed&=value;}
        async Task WaitFor(Func<bool> value){for(int i=0;i<100&&!value();i++)await Task.Delay(200);}
        try
        {
            await Task.Delay(700);_notes.Add("UI graph source",new string('Z',110000)+"GRAPH_SOURCE_END",new[]{"qa"});_windowActive=true;Navigate("brain");
            Check("One assistant page without tabs",_unifiedRoot is not null&&!Descendants(_unifiedRoot).OfType<TabView>().Any());
            Check("Conversation is embedded in the assistant page",_assistantWindow is not null&&_conversationHost!.Children.Count==1);
            Check("Precise skills and all nine roles remain available",AgentCatalog.All.Length==9&&_agentSkills.Count>=39&&_teamSpecialistChoices.Count==7);
            Check("Terminal is read-only and contains real timestamped events",_activityTerminal!.IsReadOnly&&_activityTerminal.Text.Contains("[READY]"));
            await WaitFor(()=>_brainGraphView?.CoreWebView2 is not null&&!_brainGraphStarting);
            for(int i=0;i<80;i++){if(_brainGraphView?.CoreWebView2 is not null&&await _brainGraphView.CoreWebView2.ExecuteScriptAsync("Boolean(window.astraGraph && window.astraGraph.state.nodes > 0)")=="true")break;await Task.Delay(200);}
            Check("Graph loads automatically with actual local nodes",_brainGraphView?.CoreWebView2 is not null&&await _brainGraphView.CoreWebView2.ExecuteScriptAsync("Boolean(window.astraGraph && window.astraGraph.state.nodes > 0)")=="true");
            if(_brainGraphView?.CoreWebView2 is not null)
            {
                var sourceNode=_graphSources.Values.Single(n=>n.Label=="UI graph source");
                await _brainGraphView.CoreWebView2.ExecuteScriptAsync("window.astraGraph.inspect("+JsonSerializer.Serialize(sourceNode.Id)+");document.getElementById('focusNode').click();");await Task.Delay(200);
                Check("Focus-to-chat preserves complete original node beyond graph preview",_assistantWindow!.CurrentSource.EndsWith("GRAPH_SOURCE_END")&&_assistantWindow.CurrentSource.Length>110000);
                BeginConversationActivity(["study"],"UI animation fixture · no model requested");await Task.Delay(200);
                Check("Working state reaches the graph animation",await _brainGraphView.CoreWebView2.ExecuteScriptAsync("window.astraGraph.state.working")=="true");
                EndConversationActivity();await Task.Delay(100);
                Check("Idle graph stops recurring work animation",await _brainGraphView.CoreWebView2.ExecuteScriptAsync("window.astraGraph.state.animation")=="false");
            }
            var old=_brainGraphView;Navigate("home");
            Check("Changing page closes and removes the graph control",_brainGraphView is null&&_graphHost!.Children.Count==0);
            Navigate("brain");await WaitFor(()=>_brainGraphView?.CoreWebView2 is not null&&!_brainGraphStarting);
            Check("Returning creates a fresh graph instance",_brainGraphView is not null&&!ReferenceEquals(old,_brainGraphView));
            _windowActive=false;UpdateUnifiedVisuals();
            Check("Another foreground app releases graph resources",_brainGraphView is null&&_graphHost!.Children.Count==0);
            _windowActive=true;Navigate("orbit");await WaitFor(()=>_orbitReady);
            Check("Desktop 3D starts automatically and reports a real rendered scene",_orbitActive&&_orbitReady&&_orbitView is not null);
            Check("Desktop nucleus contains the animated star, corona and gimbal geometry",_orbitView?.CoreWebView2 is not null&&await _orbitView.CoreWebView2.ExecuteScriptAsync("Boolean(window.astraOrbitDiagnostics && window.astraOrbitDiagnostics().coreParts >= 3 && window.astraOrbitDiagnostics().gimbals > 0)")=="true");
            _windowActive=false;UpdateUnifiedVisuals();
            Check("Another foreground app releases desktop 3D",_orbitView is null&&!_orbitActive&&OrbitViewHost.Children.Count==0);
            _windowActive=true;Navigate("brain");
            Check("Auto routing selects specialists for an orbital deliverable",PlanConversation("Realizza un progetto Python di simulazione orbitale").UseTeam);
            _assistantWindow!.SetInput(new string('X',110000)+"SOURCE_END");_assistantWindow.SetMessage(new string('Y',110000)+"GOAL_END");
            Check("Embedded conversation retains long input",_assistantWindow.CurrentGoal.EndsWith("GOAL_END")&&_assistantWindow.CurrentSource.EndsWith("SOURCE_END"));
            _assistantWindow.SetInput("");_assistantWindow.SetMessage("");
            if(Environment.GetEnvironmentVariable("ASTRA_UNIFIED_LIVE_PROBE")=="1")
            {
                await _assistantWindow.SubmitForVerificationAsync("Prepara due flashcards per lo studio, usando soltanto la fonte allegata. Conserva il codice 75731 e il periodo indicato. Cita la fonte. Non inventare misure o test.","Fonte di calibrazione: il codice è 75731. Nel testo di questa fixture il periodo dichiarato è 90 secondi. Si tratta di dati sintetici per un test software, non di una misura fisica.");
                var job=_selectedTeamJob is null?null:_teamStore.Records.Get(_selectedTeamJob);
                var live=new{passed=job?.Status=="Completed"&&_assistantWindow.LastResponse.Contains("75731")&&_assistantWindow.LastResponse.Contains("90"),provider=_teamStore.Configuration.UseCodex?"Codex ChatGPT plan":"OpenRouter",jobId=job?.Id,status=job?.Status,handoffs=job?.Steps.Select(s=>new{s.AgentId,s.Status}),replyCharacters=_assistantWindow.LastResponse.Length,reply=_assistantWindow.LastResponse};
                await File.WriteAllTextAsync(Path.Combine(AstraPaths.UserDataRoot,"conversation-live-probe.json"),JsonSerializer.Serialize(live,new JsonSerializerOptions{WriteIndented=true}));
            }
        }
        catch(Exception ex){Check("Unhandled UI failure: "+ex,false);}
        bool liveRequested=Environment.GetEnvironmentVariable("ASTRA_UNIFIED_LIVE_PROBE")=="1";
        var report=new{passed,modelTested=liveRequested,networkTested=liveRequested,utc=DateTimeOffset.UtcNow,checks};
        await File.WriteAllTextAsync(Path.Combine(AstraPaths.UserDataRoot,"assistant-unified-smoke.json"),JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
        static IEnumerable<DependencyObject> Descendants(DependencyObject root){for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);yield return child;foreach(var item in Descendants(child))yield return item;}}
    }
}
