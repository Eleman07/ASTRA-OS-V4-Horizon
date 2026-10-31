using System.Text.Json;
using AstraOS.Core;
using AstraOS.Desktop.Ai;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace AstraOS.Desktop;
public sealed partial class MainWindow
{
    private async Task RunExpansionSmokeAsync(Action<string,bool,string?> check)
    {
        Navigate("home");await RefreshHomeFilesAsync();
        check("Home actual icon tiles",_homeFiles?.Items.Count==FileExplorerService.ReadDesktop().Count&&_homeFiles.Items.OfType<GridViewItem>().All(i=>i.Tag is ExplorerEntry),null);
        foreach(var route in new[]{"widgets","windows-tools","assistant","multitasking"}){Navigate(route);check("New section "+route,_v4LastPage==route&&PageTitle.Text.Length>0,null);}
        await RefreshWindowsToolsAsync();
        check("Windows tools deduplicated",_windowsTools.Count>30&&_windowsTools.Select(t=>t.Target.ToUpperInvariant()).Distinct().Count()==_windowsTools.Count,_windowsTools.Count+" actual installed tools and settings");
        check("Seven Windows categories",WindowsToolCatalog.Categories.Length==7&&_windowsTools.All(t=>WindowsToolCatalog.Categories.Contains(t.Category)),null);
        Navigate("widgets");var w=new WidgetSpec{Title=AstraOS.Core.UiText.T("QA automation"),Kind="automation",Automation=new AutomationSpec{Enabled=true,Action="notify",Argument="QA one shot",NextDue=DateTimeOffset.Now.AddSeconds(-1)}};AddWidget(w);await TickWidgetsAsync();int events=_widgetEvents.Count;await TickWidgetsAsync();
        check("Widget automation executes once",events>0&&_widgetEvents.Count==events&&_widgetStore.Load().Single(x=>x.Id==w.Id).Automation!.NextDue>DateTimeOffset.Now,null);
        _widgets.RemoveAll(x=>x.Id==w.Id);_widgetStore.Save(_widgets);RenderWidgets();
        Navigate("multitasking");var own=WindowWorkspace.List().SingleOrDefault(x=>x.ProcessId==Environment.ProcessId);var original=AppWindow.Size;
        check("Actual Windows window discovery",own is not null,null);
        if(own is not null){_windowWorkspace.Tile(new[]{own},new[]{new TileRectangle(AppWindow.Position.X,AppWindow.Position.Y,1000,780)});await Task.Delay(100);check("Native window tiling",AppWindow.Size.Width==1000&&AppWindow.Size.Height==780,null);_windowWorkspace.Restore();await Task.Delay(100);check("Native placement restored",AppWindow.Size.Width==original.Width&&AppWindow.Size.Height==original.Height,null);}
        Navigate("stem");_studioScene=SimulationLab.Run("orbit").Scene;await StartStudioAsync();
        _modelLibrary=ModelLibrary.Load();
        foreach(var m in _modelLibrary)
        {
            await LoadLibraryModelAsync(m);var deadline=DateTimeOffset.UtcNow.AddSeconds(15);while(!_studioModelReady&&_studioView is not null&&DateTimeOffset.UtcNow<deadline)await Task.Delay(80);
            check("PBR GLB runtime "+m.Id,_studioModelReady,_modelState?.Text);
        }
        foreach(var (name,text) in new[]{("qa-triangle.obj","v 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 3\n"),("qa-triangle.stl","solid triangle\nfacet normal 0 0 1\nouter loop\nvertex 0 0 0\nvertex 1 0 0\nvertex 0 1 0\nendloop\nendfacet\nendsolid triangle")})
        {
            _simulationResult=null;_simulationId=null;_studioMesh=MeshImport.Parse(name,System.Text.Encoding.UTF8.GetBytes(text));_studioLibraryModel=null;await ShowModelAsync();var deadline=DateTimeOffset.UtcNow.AddSeconds(10);while(!_studioModelReady&&DateTimeOffset.UtcNow<deadline)await Task.Delay(80);check("Personal mesh import runtime "+name,_studioModelReady,_modelState?.Text);
        }
        _studioMesh=null;
        foreach(var tool in SimulationLab.Tools)
        {
            var result=SimulationLab.Run(tool.Id);_studioScene=result.Scene;await SetSimulationModelAsync(tool.Id,result,tool.Parameters.ToDictionary(p=>p.Id,p=>p.Default));var deadline=DateTimeOffset.UtcNow.AddSeconds(10);while(!_studioModelReady&&DateTimeOffset.UtcNow<deadline)await Task.Delay(80);
            if(_studioView?.CoreWebView2 is {} core)await core.ExecuteScriptAsync("document.getElementById('model-timeline').value='500';document.getElementById('model-timeline').dispatchEvent(new Event('input'));");
            check("Numerical mesh replay "+tool.Id,_studioModelReady,_modelState?.Text);
        }
        // Plain text and synthesized speech fixtures contain no microphone or user content.
        string txt=Path.Combine(AstraPaths.UserDataRoot,"qa-input.txt");await File.WriteAllTextAsync(txt,"ASTRA known document 42");var document=await AssistantMedia.ReadAsync(txt,_windowLifetime.Token);check("Assistant text import",document.Text=="ASTRA known document 42",null);
        string fixture=Path.Combine(Path.GetDirectoryName(AstraPaths.UserDataRoot)!,"qa-inputs","ocr.png");
        if(File.Exists(fixture)){var ocr=await AssistantMedia.ReadAsync(fixture,_windowLifetime.Token);check("Windows OCR with word regions",ocr.Text.Contains("ASTRA")&&ocr.Regions?.Count>0,ocr.Text);}
        var wav=await AssistantMedia.SynthesizeAsync("Questa è una prova audio. Il progetto studia la dinamica orbitale.",_windowLifetime.Token);var audio=Path.Combine(AstraPaths.UserDataRoot,"qa-synthetic.wav");await File.WriteAllBytesAsync(audio,wav);check("Assistant WAV synthesis",wav.Length>10000&&System.Text.Encoding.ASCII.GetString(wav,0,4)=="RIFF",wav.Length+" bytes");
        var transcription=await AssistantMedia.TranscribeAsync(audio,_windowLifetime.Token);check("Whisper offline speech transcription",transcription.Segments?.Count>0&&transcription.Text.Length>8,transcription.Text);check("SRT timed output",AssistantMedia.ToSrt(transcription.Segments!).Contains(" --> "),null);
        using(var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(2)))
        {
            string draft=await RunAssistantTaskAsync("Restituisci solo un oggetto JSON con nome e numero ricavati dall'input.","nome ASTRA; numero 42",true,timeout.Token);using var parsed=JsonDocument.Parse(draft);check("Included free local AI inference",parsed.RootElement.ValueKind==JsonValueKind.Object,draft);
        }
        string localized=await RunAssistantTaskAsync("Explain the word vector in one short sentence.","A vector has magnitude and direction.",false,_windowLifetime.Token);
        check("AI answers in Windows language from English input",UiText.Language.StartsWith("it")?localized.Contains("vettor",StringComparison.OrdinalIgnoreCase):localized.Length>15,localized);
        _brain.Stop();OpenAssistant("ASTRA QA");check("Separate contextual assistant window",_assistantWindow is not null&&_assistantWindow.ContextTitle=="ASTRA QA",null);_assistantWindow?.Close();Activate();
        Navigate("gaming");StartGamingRecording();await RefreshLiveMetricsAsync(true);await Task.Delay(300);await RefreshLiveMetricsAsync(true);StopGamingRecording();check("Gaming captures actual system session",_gamingSamples.Count>=2&&_gamingCurrent?.Samples>=2&&File.Exists(GamingSummaryPath),GamingStatus());
        var qaHud=new SessionHudWindow(GamingStatus,_preferences.Palette);qaHud.Activate();check("Gaming Session HUD native window",qaHud.Content is not null,null);qaHud.Close();Activate();
        Navigate("creator");
        if(File.Exists(fixture)){var asset=await CreatorMedia.ReadAsync(fixture);_creativeAssets.Add(asset with{Note="QA shot"});CreatorMedia.Save(_creativeAssets);RenderCreativeAssets();check("Creator media metadata and palette",asset.Width==650&&asset.Height==160&&asset.Colors.Length>=2,string.Join(",",asset.Colors));check("Creator board encrypted persistence and storyboard",CreatorMedia.Load().Any(x=>x.Note=="QA shot")&&CreatorStoryboard().Contains("QA shot"),null);}
        var cursorFiles=ThemeCursorService.Generate(_preferences.Palette,48);check("All themed cursors load in Windows",cursorFiles.Count==15&&cursorFiles.Values.All(ThemeCursorService.VerifyCursorFile),"Generated and loaded only; Windows cursor scheme was not changed");
        check("Technical information is in collapsed disclosures",VisualDescendants(ThemePanel).OfType<Expander>().Any(x=>!x.IsExpanded),null);
        check("Modest hardware policy",PerformancePolicy.IsConstrained(4096,8)&&PerformancePolicy.InferenceThreads(4096,8)<=2,null);
        Navigate("home");var beforeBannerSize=AppWindow.Size;var beforeBannerPosition=AppWindow.Position;
        foreach(int width in new[]{390,640,900,1280,1920,3440})
        {
            AppWindow.Resize(new Windows.Graphics.SizeInt32(width,Math.Min(1080,Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(AppWindow.Id,Microsoft.UI.Windowing.DisplayAreaFallback.Primary).WorkArea.Height)));await Task.Delay(180);UpdateResponsiveLayout();await Task.Delay(100);
            check("Responsive banner at "+width+"px",BannerFrame.ActualWidth>0&&BannerFrame.ActualWidth<=AppRoot.ActualWidth&&BannerCopy.ActualWidth+BannerCopy.Margin.Left+BannerCopy.Margin.Right<=BannerFrame.ActualWidth+2&&BannerCopy.ActualHeight+BannerCopy.Margin.Top+BannerCopy.Margin.Bottom<=BannerFrame.ActualHeight+2,$"window={AppRoot.ActualWidth:F0}; banner={BannerFrame.ActualWidth:F0}x{BannerFrame.ActualHeight:F0}; copy={BannerCopy.ActualWidth:F0}x{BannerCopy.ActualHeight:F0}");
        }
        AppWindow.Resize(beforeBannerSize);AppWindow.Move(beforeBannerPosition);await Task.Delay(180);
        Navigate("home");check("UI follows Windows language",AppRoot.Language==UiText.Language,UiText.Language);
    }
}
