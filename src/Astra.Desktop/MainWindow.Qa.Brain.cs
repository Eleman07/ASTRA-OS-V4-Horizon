using AstraOS.Core;
using AstraOS.Core.SecondBrain;
using UglyToad.PdfPig.Writer;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;

namespace AstraOS.Desktop;
public sealed partial class MainWindow
{
    private async Task RunBrainSmokeAsync(Action<string,bool> check)
    {
        string path=Path.Combine(AstraPaths.UserDataRoot,"brain-reference.pdf");
        using(var pdf=new PdfDocumentBuilder())
        {
            var font=pdf.AddStandard14Font(Standard14Font.Helvetica);
            for(int i=1;i<=121;i++)pdf.AddPage(PageSize.A4).AddText(i==121?"Calibrationmarker: the calibration number is 73042.":$"Page {i}. General study material.",14,new PdfPoint(40,740),font);
            await File.WriteAllBytesAsync(path,pdf.Build());
        }
        var document=await ImportBrainPdfAsync(path);
        check("Second Brain imports and selects long PDFs in its library",_brainDocumentChoice?.SelectedItem is BrainDocument{Pages:121});
        var passages=await FindBrainPassagesAsync("calibrationmarker",_windowLifetime.Token);
        check("Question context includes the final PDF page",passages.Any(p=>p.Title.Contains("121")&&p.Text.Contains("73042")));
        await ExecuteBrainCommandAsync("ricorda che il laboratorio usa controlli a quaternioni",_windowLifetime.Token);
        check("Second Brain remembers user-requested facts locally",_notes.Load().Any(n=>n.Content.Contains("quaternioni")));
        await ExecuteBrainCommandAsync("aggiungi attività QA study chapter",_windowLifetime.Token);
        check("Second Brain adds actual encrypted tasks",_v4TaskStore.Load().Any(t=>t.Text=="QA study chapter"));
        var briefing=await ExecuteBrainCommandAsync("briefing",_windowLifetime.Token);
        check("Daily briefing uses real saved tasks",briefing?.Contains("QA study chapter")==true);
        await ExecuteBrainCommandAsync("focus 2 minuti",_windowLifetime.Token);
        check("Voice-compatible focus command creates a real timer",_widgets.Any(w=>w.Kind=="focus"));
        OpenAssistant("Second Brain QA");
        check("Conversational assistant keeps the Second Brain name",_assistantWindow?.Title.Contains("Second Brain")==true);
        _assistantWindow?.Close();Navigate("brain");
        check("Advanced runtime configuration starts collapsed",!_assistantMemoryPanel!.Children.OfType<Microsoft.UI.Xaml.Controls.Expander>().First(e=>ReferenceEquals(e.Content,BrainRuntimeCard)).IsExpanded);
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(_windowLifetime.Token);timeout.CancelAfter(TimeSpan.FromMinutes(3));
        await EnsureAssistantModelAsync(timeout.Token);
        string answer=await _brain.AskAsync("What is the calibration number? Answer with the number from the provided PDF source.",Array.Empty<BrainNote>(),timeout.Token,passages);
        check("Included local AI answers from the indexed PDF evidence",answer.Contains("73042"));
        _brain.Stop();
    }
}
