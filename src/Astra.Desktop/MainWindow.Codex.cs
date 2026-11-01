using AstraOS.Core;
using AstraOS.Desktop.Ai;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AstraOS.Desktop;
public sealed partial class MainWindow
{
    private UIElement BuildCodexControls()
    {
        var settings=_teamStore.Configuration;
        var panel=new StackPanel{Spacing=12};
        panel.Children.Add(TeamHeading("Codex · il tuo account, la tua squadra",21));
        panel.Children.Add(Label("ASTRA usa il login ChatGPT gestito da Codex. Non salva token o un account fisso. Ogni nuovo incarico verifica l'accesso attivo; tutti gli agenti condividono le quote del piano. Nessun modello viene eseguito sulla tua GPU.",12));
        var account=Label("Verifica dell'account attivo…",12);panel.Children.Add(account);
        async Task RefreshAccount(){try{var current=await CodexBridge.ReadAccountAsync(_teamStore.Configuration.CodexExecutablePath,_windowLifetime.Token);account.Text=current.DisplayName+" · "+current.Plan+"\n"+current.Quota;}catch(Exception ex){account.Text=ex.Message;}}
        panel.Children.Add(Action("Sincronizza account e disponibilità",async(_,_)=>await RefreshAccount()));
        var options=new StackPanel{Spacing=10};
        var model=new TextBox{Header="Modello Codex · scegli un modello incluso nel tuo piano",Text=settings.CodexModel,PlaceholderText="gpt-6.1-sol"};options.Children.Add(model);
        var effort=new ComboBox{Header="Profondità del ragionamento",ItemsSource=new[]{"low","medium","high","xhigh"},SelectedItem=settings.CodexReasoningEffort,HorizontalAlignment=HorizontalAlignment.Stretch};options.Children.Add(effort);
        options.Children.Add(Action("Usa Codex per assistente e agenti",async(_,_)=>{try{if(string.IsNullOrWhiteSpace(model.Text)||model.Text.Any(c=>!char.IsAsciiLetterOrDigit(c)&&c is not '-' and not '_' and not '.'))throw new ArgumentException("Inserisci un identificatore di modello Codex valido.");var c=_teamStore.Configuration;c.CodexModel=model.Text.Trim();c.CodexReasoningEffort=effort.SelectedItem as string??"xhigh";_ = await CodexBridge.ReadAccountAsync(c.CodexExecutablePath,_windowLifetime.Token);c.UseCodex=true;c.PreferFreeCloud=true;c.UseGpu=false;c.FallbackToGpu=false;c.OutputTokens=0;SaveAssistantRuntimeSettings(c);_brain.Stop();_teamStatus!.Text="Codex è attivo per chat e squadra. Account gestito dall'app Codex; nessun fallback automatico a OpenRouter o alla GPU.";await RefreshAccount();}catch(Exception ex){_teamStatus!.Text=ex.Message;}}));
        panel.Children.Add(new Expander{Header="Modello e ragionamento",Content=options,HorizontalAlignment=HorizontalAlignment.Stretch});
        var timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(30)};bool refreshing=false;
        timer.Tick+=async(_,_)=>{if(refreshing||!_teamStore.Configuration.UseCodex||!_windowActive||_v4LastPage!="brain")return;refreshing=true;try{await RefreshAccount();}finally{refreshing=false;}};
        panel.Loaded+=async(_,_)=>{if(Environment.GetEnvironmentVariable("ASTRA_QA_MODE")=="1"){account.Text="Account synchronization is suppressed in isolated UI QA.";return;}timer.Start();await RefreshAccount();};panel.Unloaded+=(_,_)=>timer.Stop();
        return new Expander{Header="Codex · account, modello e disponibilità",Content=panel,HorizontalAlignment=HorizontalAlignment.Stretch};
    }
}
