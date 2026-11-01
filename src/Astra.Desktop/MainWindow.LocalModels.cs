using System.Security.Cryptography;
using AstraOS.Core;
using AstraOS.Core.Agents;
using AstraOS.Desktop.Ai;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace AstraOS.Desktop;

public sealed partial class MainWindow
{
    private ComboBox? _localModelPicker;
    private ComboBox? _localProviderPicker;
    private TextBox? _localModelPathView;
    private NumberBox? _localContextView;
    private void SyncLocalModelControls()
    {
        var c=_teamStore.Configuration;
        if(_localModelPathView is not null)_localModelPathView.Text=c.ModelPath;
        if(_localContextView is not null)_localContextView.Value=c.ContextSize;
        if(_localModelPicker is not null)
        {
            var selected=_localModelPicker.Items.OfType<ComboBoxItem>().FirstOrDefault(item=>item.Tag is LocalModelSpec m && (m.Id==c.SelectedModelId || Path.GetFileName(c.ModelPath).Equals(m.FileName,StringComparison.OrdinalIgnoreCase)));
            if(selected is not null)_localModelPicker.SelectedItem=selected;else if(string.IsNullOrWhiteSpace(c.ModelPath)&&string.IsNullOrWhiteSpace(c.SelectedModelId))_localModelPicker.SelectedIndex=-1;
        }
    }
    private UIElement BuildLocalModelControls()
    {
        var panel=new StackPanel{Spacing=9}; var saved=_teamStore.Configuration;
        panel.Children.Add(TeamHeading("Your model. Your GPU.",18));
        panel.Children.Add(Label("Choose the model yourself. Local inference has no API key, per-day request quota or paid fallback. Context and speed depend on your GPU. The verified catalog uses OpenAI/Microsoft (US) and Mistral AI (France).",12));
        var provider=new ComboBox{Header="Inference provider",ItemsSource=new[]{"Local GPU · no API quota","OpenRouter · optional online service","Codex · il tuo piano ChatGPT"},SelectedIndex=saved.UseCodex?2:saved.PreferFreeCloud?1:0,HorizontalAlignment=HorizontalAlignment.Stretch};
        _localProviderPicker=provider;
        provider.SelectionChanged+=(_,_)=>{if(_loadingAiSettings)return;var c=_teamStore.Configuration;c.UseCodex=provider.SelectedIndex==2;c.PreferFreeCloud=provider.SelectedIndex!=0;c.UseGpu=!c.PreferFreeCloud;c.FallbackToGpu=false;SaveAssistantRuntimeSettings(c);_brain.Stop();_teamStatus!.Text=c.UseCodex?"Codex selezionato. Usa il login ChatGPT e le quote del tuo piano.":c.PreferFreeCloud?"Online provider selected explicitly. External quotas apply.":"Local GPU selected. Choose verified model weights below.";};panel.Children.Add(provider);
        var choice=new ComboBox{Header="Verified local language model",HorizontalAlignment=HorizontalAlignment.Stretch,PlaceholderText="Select your model"};
        _localModelPicker=choice;
        foreach(var model in LocalModelCatalog.TextModels){var item=new ComboBoxItem{Content=model.Label+" · "+model.Publisher+" · "+(model.Bytes/1073741824.0).ToString("F2")+" GiB weights",Tag=model};choice.Items.Add(item);if(model.Id==saved.SelectedModelId || Path.GetFileName(saved.ModelPath).Equals(model.FileName,StringComparison.OrdinalIgnoreCase))choice.SelectedItem=item;}
        var path=new TextBox{Header="Selected GGUF file",Text=saved.ModelPath,IsReadOnly=true,TextWrapping=TextWrapping.Wrap};
        var context=new NumberBox{Header="Local context tokens · input, evidence, reasoning and output share this window",Minimum=1024,Value=saved.ContextSize,HorizontalAlignment=HorizontalAlignment.Stretch};
        _localModelPathView=path;_localContextView=context;
        void SelectPath(string file,LocalModelSpec model){var c=_teamStore.Configuration;c.ModelPath=file;c.SelectedModelId=model.Id;c.UseCodex=false;c.PreferFreeCloud=false;c.UseGpu=true;c.FallbackToGpu=false;if(string.IsNullOrWhiteSpace(c.RuntimePath))c.RuntimePath=GpuRuntime.Executable;c.OutputTokens=0;c.ContextSize=Math.Min(32768u,model.NativeContext);SaveAssistantRuntimeSettings(c);path.Text=file;context.Value=c.ContextSize;provider.SelectedIndex=0;_brain.Stop();_teamStatus!.Text="Selected "+model.Label+". GPU memory and actual test results determine suitability; model quality is not assumed equivalent to a hosted frontier model.";}
        choice.SelectionChanged+=(_,_)=>
        {
            if(_loadingAiSettings)return;
            if(choice.SelectedItem is not ComboBoxItem{Tag:LocalModelSpec model})return;
            var current=_teamStore.Configuration;
            if(Path.GetFileName(current.ModelPath).Equals(model.FileName,StringComparison.OrdinalIgnoreCase)&&File.Exists(current.ModelPath)){SelectPath(current.ModelPath,model);return;}
            if(File.Exists(model.Path)){SelectPath(model.Path,model);return;}
            current.SelectedModelId=model.Id;current.ModelPath="";current.UseCodex=false;current.PreferFreeCloud=false;current.UseGpu=true;current.ContextSize=Math.Min(32768u,model.NativeContext);SaveAssistantRuntimeSettings(current);_brain.Stop();
            _teamStatus!.Text="Download "+model.Label+" or choose its verified GGUF file. A previous model will not be used instead.";
        };
        panel.Children.Add(choice);panel.Children.Add(path);
        panel.Children.Add(Action("Choose a verified GGUF file…",async(_,_)=>{try{var picker=new FileOpenPicker();picker.FileTypeFilter.Add(".gguf");WinRT.Interop.InitializeWithWindow.Initialize(picker,WinRT.Interop.WindowNative.GetWindowHandle(this));var file=await picker.PickSingleFileAsync();if(file is null)return;_teamStatus!.Text="Verifying model publisher and SHA-256…";var model=await LocalModelCatalog.VerifyTextModelAsync(file.Path,_windowLifetime.Token);SelectPath(file.Path,model);}catch(Exception ex){_teamStatus!.Text=ex.Message;}}));
        panel.Children.Add(Action("Download selected model from its pinned source",async(sender,_)=>
        {
            if(choice.SelectedItem is not ComboBoxItem{Tag:LocalModelSpec model}){_teamStatus!.Text="Select a model first.";return;}
            if(sender is Button button)button.IsEnabled=false;string? temporary=null;
            try
            {
                SafeFileOperations.NoLinks(model.Path);Directory.CreateDirectory(Path.GetDirectoryName(model.Path)!);
                if(File.Exists(model.Path)){await LocalModelCatalog.VerifyTextModelAsync(model.Path,_windowLifetime.Token);SelectPath(model.Path,model);return;}
                temporary=model.Path+".download-"+Guid.NewGuid().ToString("N");
                using var client=new HttpClient{Timeout=Timeout.InfiniteTimeSpan};using var response=await client.GetAsync(model.Url,HttpCompletionOption.ResponseHeadersRead,_windowLifetime.Token);response.EnsureSuccessStatusCode();
                await using(var input=await response.Content.ReadAsStreamAsync(_windowLifetime.Token))
                await using(var output=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None,1024*1024,true))
                {var buffer=new byte[1024*1024];long received=0;DateTime last=DateTime.MinValue;int read;while((read=await input.ReadAsync(buffer,_windowLifetime.Token))>0){received+=read;if(received>model.Bytes)throw new InvalidDataException("Download exceeded the pinned model size.");await output.WriteAsync(buffer.AsMemory(0,read),_windowLifetime.Token);if((DateTime.UtcNow-last).TotalSeconds>1){_teamStatus!.Text="Downloading "+model.Label+" · "+(100.0*received/model.Bytes).ToString("F1")+"%";last=DateTime.UtcNow;}}}
                await LocalModelCatalog.VerifyTextModelAsync(temporary,_windowLifetime.Token);File.Move(temporary,model.Path,false);temporary=null;SelectPath(model.Path,model);
            }
            catch(Exception ex){_teamStatus!.Text=ex.Message;}
            finally{if(temporary is not null&&File.Exists(temporary))File.Delete(temporary);if(sender is Button b)b.IsEnabled=true;}
        }));
        panel.Children.Add(context);panel.Children.Add(Action("Save local context and complete-output settings",(_,_)=>{try{if(!double.IsFinite(context.Value)||context.Value<1024||context.Value>uint.MaxValue||context.Value!=Math.Truncate(context.Value))throw new ArgumentException("Choose a valid context size supported by your model and GPU.");var c=_teamStore.Configuration;c.ContextSize=(uint)context.Value;c.OutputTokens=0;c.UseGpu=true;SaveAssistantRuntimeSettings(c);_brain.Stop();_teamStatus!.Text="Saved. ASTRA preserves full input and requests generation until the model stops; context overflow is reported explicitly.";}catch(Exception ex){_teamStatus!.Text=ex.Message;}}));
        panel.Children.Add(Label("Language models are downloaded separately and verified before use. Chat, agents and image analysis share the GPU and take turns. Choose a smaller model when other apps need more GPU memory.",12));
        var speech=new ComboBox{Header="Local speech recognition · OpenAI Whisper",HorizontalAlignment=HorizontalAlignment.Stretch,ItemsSource=new[]{"small · accuracy focused","tiny · faster, lower accuracy"},SelectedIndex=saved.SpeechModelId=="tiny"?1:0};
        speech.SelectionChanged+=(_,_)=>{var c=_teamStore.Configuration;c.SpeechModelId=speech.SelectedIndex==1?"tiny":"small";SaveAssistantRuntimeSettings(c);_teamStatus!.Text="Local speech model selected. Review transcripts, especially numbers and technical terms.";};panel.Children.Add(speech);
        var speechLanguage=new TextBox{Header="Speech language · auto or two-letter code (it, en, …)",Text=saved.SpeechLanguage};panel.Children.Add(speechLanguage);
        panel.Children.Add(Action("Save speech language",(_,_)=>{string code=speechLanguage.Text.Trim().ToLowerInvariant();if(code!="auto"&&(code.Length!=2||code.Any(c=>c<'a'||c>'z'))){_teamStatus!.Text="Use auto or a two-letter language code.";return;}var c=_teamStore.Configuration;c.SpeechLanguage=code;SaveAssistantRuntimeSettings(c);_teamStatus!.Text="Speech language saved. This setting provides language metadata, not expected transcript words.";}));
        return panel;
    }
}
