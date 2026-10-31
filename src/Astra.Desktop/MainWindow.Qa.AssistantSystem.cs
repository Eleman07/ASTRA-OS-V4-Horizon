using System.Text.Json;
using AstraOS.Core;
using AstraOS.Desktop.Ai;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AstraOS.Desktop;
public sealed partial class MainWindow
{
    private async Task RunAssistantSystemSmokeAsync()
    {
        var checks=new List<object>();bool passed=true;
        void Check(string name,bool result){checks.Add(new{name,status=result?"PASS":"FAIL"});passed&=result;}
        try
        {
            _preferences.AiProvider="local";_preferenceStore.Save(_preferences);RefreshAssistantRuntimeUi();
            if(Environment.GetEnvironmentVariable("ASTRA_QA_AI_RELOCATION")=="1")
            {
                _preferences.AiProvider="local";_preferenceStore.Save(_preferences);RefreshAssistantRuntimeUi();
                Check("Legacy runtime and model migrate into the current portable folder",AiRuntimePath.Text==BundledAi.RuntimePath&&AiModelPath.Text==BundledAi.ModelPath&&File.Exists(AiModelPath.Text));
                var saved=_preferenceStore.Load();Check("Migrated AI paths survive preference reload",saved.AiExecutablePath==BundledAi.RuntimePath&&saved.AiModelPath==BundledAi.ModelPath);
                Navigate("brain");
                Check("AI settings expose OpenRouter privacy details and offline recovery",L10n0117.Text.Contains("OPENROUTER")&&AiUseBundledButton.Content is string&& !UseAiGpuToggle.IsEnabled);
                var advanced=_assistantMemoryPanel!.Children.OfType<Expander>().Single(e=>ReferenceEquals(e.Content,BrainRuntimeCard));advanced.IsExpanded=true;
                await Task.Delay(250);AiStartButton.StartBringIntoView();
                await File.WriteAllTextAsync(Path.Combine(AstraPaths.UserDataRoot,"ai-start-ready.json"),JsonSerializer.Serialize(new{action="Click Start local model",expectedModel=BundledAi.ModelPath}));
                for(int i=0;i<180&&!_brain.IsRunning;i++)await Task.Delay(1000);
                Check("Real Start local model action loads the relocated bundled model",_brain.IsRunning);
                if(!_brain.IsRunning)throw new InvalidOperationException(AiStatusValue.Text);
                advanced.IsExpanded=false;
            }
            Navigate("system");Check("Control center is a real navigable page",_systemHubPanel?.Visibility==Visibility.Visible&&DesktopCapabilities.All.Length>=40);
            Check("Clipboard shelf starts empty and does not capture automatically",_clipboardShelf.Count==0);
            Check("Installable skill catalog is loaded in the application",_agentSkills.Count>=26&&_agentSkills.Select(s=>s.Id).Distinct().Count()==_agentSkills.Count);
            Navigate("creator");Navigate("assistant");
            Check("Creator has one ASTRA Assistant entry",BrainNav.Visibility==Visibility.Collapsed&&PageTitle.Text=="ASTRA Assistant"&&_v4LastPage=="brain");
            Check("Legacy encrypted PDF and note controls are retained",_assistantMemoryPanel!.Children.Count>2&&_brainDocumentChoice is not null&&AiQuestion is not null);
            await RunAssistantRoutineAsync(AssistantWorkspace.Routines.Single(r=>r.Id=="creator"),_windowLifetime.Token);
            Check("Creator routine creates a persisted task and a focus timer",_v4TaskStore.Load().Any(t=>t.Text.Contains("sottotitoli"))&&_widgets.Any(w=>w.Kind=="focus"));
            Navigate("gaming");Navigate("assistant");Check("Gaming uses the same assistant and memory panel",AssistantName=="ASTRA Assistant"&&BrainNav.Visibility==Visibility.Collapsed&&_assistantMemoryPanel.Children.OfType<Expander>().Any(e=>ReferenceEquals(e.Content,BrainRuntimeCard)));
            var coach=await ExecuteBrainCommandAsync("gaming coach",_windowLifetime.Token);Check("Gaming coach reports missing telemetry honestly",coach is not null&&coach.Contains(UiText.T("Avvia una registrazione dal Gaming Lab per ottenere consigli basati su misure reali.")));
            var custom=new AssistantRoutine(Guid.NewGuid().ToString("N"),"Native QA routine","QA",new[]{new AssistantAction("task","native-assistant-marker")});
            _assistantRoutineStore.Save(new[]{custom});Check("Personal routines are saved in the encrypted store",_assistantRoutineStore.Load().Single().Steps[0].Argument=="native-assistant-marker");
            await RunAssistantRoutineAsync(custom,_windowLifetime.Token);
            var page=await ReadAssistantPageAsync("https://example.com",_windowLifetime.Token);Check("A public webpage is imported as a cited source",page.Text.Contains("Example Domain")&&WebEvidenceContext().Contains("https://example.com"));
            string fixture=Path.Combine(AstraPaths.UserDataRoot,"files-fixture");Directory.CreateDirectory(fixture);await File.WriteAllTextAsync(Path.Combine(fixture,"notes.txt"),"Native assistant file fixture");
            Navigate("files");await LoadFilesAsync(fixture);_filesList!.SelectedItem=_fileEntries.Single(e=>e.Name=="notes.txt");await FileActionAsync("zip");
            Check("File Explorer creates a real ZIP from the selected item",File.Exists(Path.Combine(fixture,"notes.txt.zip")));
            _filesList.SelectedItem=_fileEntries.Single(e=>e.Name=="notes.txt.zip");await FileActionAsync("extract");Check("File Explorer extracts the selected ZIP",File.Exists(Path.Combine(fixture,"notes.txt-extracted","notes.txt")));
            await File.WriteAllTextAsync(Path.Combine(fixture,"sample.py"),"value = 1\n");_agentProjectRoot=fixture;_agentProjectFiles!.ItemsSource=ProjectWorkspaceTools.List(fixture);_agentProjectFiles.SelectedItem="sample.py";_agentProjectGoal!.Text="Change only the numeric literal 1 to 2. The exact original span to replace is 1.";
            if(Environment.GetEnvironmentVariable("ASTRA_QA_SKIP_MODEL")!="1")
            {
                using var timeout=CancellationTokenSource.CreateLinkedTokenSource(_windowLifetime.Token);timeout.CancelAfter(TimeSpan.FromMinutes(3));
                var answer=await RunAssistantTaskAsync("Reply only with the calibration code from the source.","Calibration code: 95731",false,timeout.Token);Check("Unified local AI uses actual source evidence",answer.Contains("95731"));
                await PrepareAgentEditsAsync();Check("Coding assistant prepares a real proposal without writing",_agentChangePlan is not null&&File.ReadAllText(Path.Combine(fixture,"sample.py"))=="value = 1\n");
            }
            else
            {
                _agentChangePlan=WorkspaceChangePlan.Prepare(fixture,new[]{new ProposedFileEdit("sample.py","value = 2\n")});
                Check("Manual fixture proposal is prepared without writing",File.ReadAllText(Path.Combine(fixture,"sample.py"))=="value = 1\n");
            }
            if(_agentChangePlan is not null)
            {
                Navigate("brain");var review=ApproveAgentEditsAsync();Check("Approval displays the complete original and proposed text",_agentApprovalPreview?.Text.Contains("- value = 1")==true&&_agentApprovalPreview.Text.Contains("+ value = 2"));await File.WriteAllTextAsync(Path.Combine(AstraPaths.UserDataRoot,"approval-ready.json"),JsonSerializer.Serialize(new{action="Cancel the file edit review",expected="value = 1"}));
                await review.WaitAsync(TimeSpan.FromMinutes(3));Check("Cancelling the explicit approval leaves project files untouched",File.ReadAllText(Path.Combine(fixture,"sample.py"))=="value = 1\n");
            }
            Navigate("stem");Navigate("brain");Check("STEM shares the unified ASTRA Assistant and memory",AssistantName=="ASTRA Assistant"&&BrainNav.Visibility==Visibility.Collapsed&&PageTitle.Text=="ASTRA Assistant");
            Navigate("creator");Navigate("assistant");OpenAssistant();_assistantWindow!.SetMessage("briefing");
            Check("Modern conversation window uses ASTRA Assistant",_assistantWindow.Title.Contains("ASTRA Assistant"));
            Check("Conversation is not forced above other apps",_assistantWindow.AppWindow.Presenter is OverlappedPresenter{IsAlwaysOnTop:false});
            Check("Conversation has a welcome message",_assistantWindow.ConversationMessageCount==1);
            await File.WriteAllTextAsync(Path.Combine(AstraPaths.UserDataRoot,"assistant-ready.json"),JsonSerializer.Serialize(new{title=_assistantWindow.Title,request="briefing",expected="native-assistant-marker"}));
            for(int i=0;i<180&&string.IsNullOrWhiteSpace(_assistantWindow.LastResponse);i++)await Task.Delay(1000);
            Check("Real Send action runs a command and displays the actual saved task",_assistantWindow.LastResponse.Contains("native-assistant-marker")&&_assistantWindow.ConversationMessageCount>=3);
            Check("Conversation is saved in the encrypted history",new AssistantChatStore().List().Count==1&&new AssistantChatStore().ReadPage(new AssistantChatStore().List()[0].Id).Any(m=>m.Text.Contains("native-assistant-marker")));
            _brain.Stop();
        }
        catch(Exception ex){passed=false;checks.Add(new{name="Native assistant/system exception",status="FAIL",detail=ex.ToString()});}
        finally
        {
            await File.WriteAllTextAsync(Path.Combine(AstraPaths.UserDataRoot,"assistant-system-smoke.json"),JsonSerializer.Serialize(new{passed,modelTested=Environment.GetEnvironmentVariable("ASTRA_QA_SKIP_MODEL")!="1",checks},new JsonSerializerOptions{WriteIndented=true}));
            _assistantWindow?.Close();Close();
        }
    }
}
