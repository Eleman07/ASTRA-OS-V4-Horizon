using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using AstraOS.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace AstraOS.Desktop;
public sealed class UiOptionConverter:IValueConverter
{
    public object Convert(object value,Type targetType,object parameter,string language)
    {
        string text=value is ComboBoxItem item?item.Content?.ToString()??"":value?.GetType().GetProperty("Name")?.GetValue(value)?.ToString()??value?.ToString()??"";
        return UiText.T(text);
    }
    public object ConvertBack(object value,Type targetType,object parameter,string language)=>throw new NotSupportedException();
}
public static class UiLocalization
{
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]
    private static extern bool GetUserPreferredUILanguages(uint flags,out uint count,StringBuilder? languages,ref uint length);
    public static string DetectWindowsLanguage()
    {
        var qa=Environment.GetEnvironmentVariable("ASTRA_UI_LANGUAGE");if(!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASTRA_DATA_DIRECTORY"))&&!string.IsNullOrWhiteSpace(qa))return qa;
        uint length=0;GetUserPreferredUILanguages(8,out _,null,ref length);
        if(length is >0 and <4096){var buffer=new StringBuilder((int)length);if(GetUserPreferredUILanguages(8,out _,buffer,ref length)){var first=buffer.ToString().Split('\0',StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();if(first is not null)return first;}}
        return CultureInfo.CurrentUICulture.Name;
    }
    public static void Initialize()=>UiText.Initialize(Path.Combine(AppContext.BaseDirectory,"Localization"),DetectWindowsLanguage());
}
public sealed partial class MainWindow
{
    private Dictionary<string,Dictionary<string,string>>? _xamlStrings;
    private void LocalizeXaml()
    {
        try
        {
            _xamlStrings??=JsonSerializer.Deserialize<Dictionary<string,Dictionary<string,string>>>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Localization","xaml.json")))??new();
            AppRoot.Language=UiText.Language;
            AppRoot.FlowDirection=CultureInfo.GetCultureInfo(UiText.Language).TextInfo.IsRightToLeft?FlowDirection.RightToLeft:FlowDirection.LeftToRight;
            foreach(var (name,properties) in _xamlStrings)
            {
                var element=GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(this) as DependencyObject??AppRoot.FindName(name) as DependencyObject;if(element is null)continue;
                foreach(var (property,source) in properties)
                {
                    string value=UiText.T(source);
                    if(property=="ToolTipService.ToolTip")ToolTipService.SetToolTip(element,value);
                    else if(property=="AutomationProperties.Name")Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(element,value);
                    else if(element.GetType().GetProperty(property) is {} info && info.GetValue(element) is string current && (current==source||current==UiText.English.GetValueOrDefault(source)))info.SetValue(element,value);
                }
            }
            LocalizeCombos(AppRoot);
        }
        catch(Exception ex){StartupDiagnostics.Record("Localization.optional",ex.Message);}
    }
    private void RelocalizeControls(DependencyObject root)
    {
        if(root is GridViewItem{Tag:ExplorerEntry}||root is ListViewItem)return;
        // Only interface properties; never edit document/input values or file names in item sources.
        if(root is TextBlock label&&!label.IsTextSelectionEnabled)label.Text=UiText.T(UiText.SourceFor(label.Text));
        foreach(var name in new[]{"Header","PlaceholderText","OnContent","OffContent","Content"})
        {
            var p=root.GetType().GetProperty(name);if(p?.CanWrite==true&&p.GetValue(root) is string s)p.SetValue(root,UiText.T(UiText.SourceFor(s)));
        }
        if(root is ComboBox combo&&combo.ItemTemplate is {} template){combo.ItemTemplate=null;combo.ItemTemplate=template;}
        for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)RelocalizeControls(VisualTreeHelper.GetChild(root,i));
    }
    private bool _interfaceTranslationBusy;
    private void InitializeInterfaceLanguage()
    {
        var p=new StackPanel{Spacing=8};p.Children.Add(Label(AstraOS.Core.UiText.T("LINGUA DI WINDOWS"),18));
        var state=Label(UiText.LanguageName+" · "+(UiText.HasCatalog?UiText.T("Interfaccia pronta"):UiText.T("Preparazione della traduzione locale alla prima apertura")),12);p.Children.Add(state);
        async Task Prepare()
        {
            if(UiText.HasCatalog||_interfaceTranslationBusy)return;_interfaceTranslationBusy=true;
            try{await EnsureAssistantModelAsync(_windowLifetime.Token);await Ai.InterfaceTranslator.CreateAsync(_brain,(n,total)=>{DispatcherQueue.TryEnqueue(()=>state.Text=UiText.LanguageName+$" · {n}/{total}");},_windowLifetime.Token);UiText.Reload();LocalizeXaml();RelocalizeControls(AppRoot);state.Text=UiText.LanguageName+" · "+UiText.T("Interfaccia pronta");}
            catch(OperationCanceledException){}catch(Exception ex){state.Text=UiText.LanguageName+" · "+ex.Message;StartupDiagnostics.Record("Localization.generate.failed",ex.Message);}
            finally{_interfaceTranslationBusy=false;}
        }
        p.Children.Add(Label(AstraOS.Core.UiText.T("La lingua segue le preferenze dell'account Windows. Per le lingue senza catalogo incluso, avvia la traduzione solo quando lo scegli: con OpenRouter i testi dell'interfaccia vengono inviati al servizio e il provider può conservarli secondo le proprie policy; puoi usare l'AI locale offline nelle impostazioni."),12));
        p.Children.Add(Action(AstraOS.Core.UiText.T("Prepara / riprova traduzione locale"),async(_,_)=>await Prepare()));ThemePanel.Children.Add(Card(p));
    }
    private void LocalizeCombos(DependencyObject root)
    {
        if(root is ComboBox combo&&combo.ItemTemplate is null&&AppRoot.Resources.TryGetValue("UiOptionTemplate",out var template))
        {combo.DisplayMemberPath="";combo.ItemTemplate=(DataTemplate)template;}
        for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)LocalizeCombos(VisualTreeHelper.GetChild(root,i));
    }
}
