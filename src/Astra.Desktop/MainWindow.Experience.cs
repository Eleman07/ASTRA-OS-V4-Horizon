using AstraOS.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace AstraOS.Desktop;
public sealed partial class MainWindow
{
    private bool _lightDevice;
    private void ApplyHardwareDefaults()
    {
        var snapshot=SystemSnapshot.Capture();_lightDevice=PerformancePolicy.IsConstrained(snapshot.PhysicalMemoryMb,Environment.ProcessorCount);
        if(_lightDevice&&!File.Exists(_preferenceStore.FilePath)){_preferences.EcoMode=true;_preferences.OrbitFpsCap=15;_preferences.ReducedMotion=true;}
    }
    private Expander Details(string title,UIElement content)
    {
        var disclosure=new Expander{Header=UiText.T(title),Content=content,HorizontalAlignment=HorizontalAlignment.Stretch,CornerRadius=new CornerRadius(12),Margin=new Thickness(0,3,0,3)};
        disclosure.Expanding+=(_,_)=>DispatcherQueue.TryEnqueue(()=>AnimateDisclosure(content));return disclosure;
    }
    private void SimplifyTechnicalPanels(UIElement root)
    {
        if(root is Expander)return;
        if(root is StackPanel panel)
        {
            for(int i=0;i<panel.Children.Count;i++)
            {
                var child=panel.Children[i];if(child is TextBlock text&&text.Text.Length>170&&!text.IsTextSelectionEnabled&&IsTechnicalCopy(text.Text))
                {
                    panel.Children.RemoveAt(i);panel.Children.Insert(i,Details(AstraOS.Core.UiText.T("Dettagli e informazioni"),text));
                }
                else SimplifyTechnicalPanels(child);
            }
        }
        else if(root is Border{Child:UIElement borderChild})SimplifyTechnicalPanels(borderChild);
        else if(root is Grid grid)foreach(var gridChild in grid.Children)SimplifyTechnicalPanels(gridChild);
    }
    private static bool IsTechnicalCopy(string text)=>new[]{"DPAPI","BM25","JSON","CSV","WebGL","CFD","FEM","WinGet","runtime","gguf","solutore","RK4","FTCS","localhost","quaternion","shader","in-process","hash","codec","metadata","metadati","technical"}.Any(word=>text.Contains(word,StringComparison.OrdinalIgnoreCase));
    private void InitializeExperience()
    {
        foreach(var panel in new[]{HomePanel,ProfilesPanel,CatalogPanel,DiagnosticPanel,SecurityPanel,BrainPanel,PluginsPanel,UbuntuPanel,ThemePanel,AssistantPanel,MultitaskPanel,WindowsToolsPanel,WidgetsPanel})SimplifyTechnicalPanels(panel);
        var p=new StackPanel{Spacing=8};p.Children.Add(Label(AstraOS.Core.UiText.T("PRESTAZIONI SU MISURA"),19));p.Children.Add(Label(_lightDevice?AstraOS.Core.UiText.T("Per questo dispositivo è consigliata la modalità leggera. Puoi personalizzare movimento, grafica e aggiornamenti."):AstraOS.Core.UiText.T("Scegli il livello di leggerezza adatto al dispositivo."),12));
        p.Children.Add(Action(AstraOS.Core.UiText.T("Modalità leggera"),(_,_)=>{_preferences.ReducedMotion=true;_preferences.OrbitFpsCap=15;SetQuietMode(true);_settingToggles=true;ReduceMotionToggle.IsOn=V4MotionToggle.IsOn=true;OrbitFpsCombo.SelectedIndex=0;_settingToggles=false;ResetMotion();_preferenceStore.Save(_preferences);}));
        p.Children.Add(Action(AstraOS.Core.UiText.T("Modalità bilanciata"),(_,_)=>{_preferences.ReducedMotion=false;_preferences.OrbitFpsCap=30;SetQuietMode(false);_settingToggles=true;ReduceMotionToggle.IsOn=V4MotionToggle.IsOn=false;OrbitFpsCombo.SelectedIndex=1;_settingToggles=false;_preferenceStore.Save(_preferences);}));
        p.Children.Add(Details(AstraOS.Core.UiText.T("Come ASTRA gestisce le risorse"),Label(AstraOS.Core.UiText.T("Al primo avvio, RAM fino a 8 GB o fino a 4 processori logici attivano una configurazione leggera. Le preferenze già salvate vengono rispettate. I renderer 3D sono caricati su richiesta e rilasciati uscendo dalla pagina. Eco usa aggiornamenti ogni 15 s e riduce la risoluzione dei canvas. L'AI carica il modello solo quando serve e si arresta dal pulsante dedicato. La generazione automatica di traduzioni su dispositivi limitati si avvia manualmente."),12)));ThemePanel.Children.Add(Card(p));
    }
}
