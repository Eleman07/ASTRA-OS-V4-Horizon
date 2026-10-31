using AstraOS.Core.SecondBrain;
using AstraOS.Desktop.Ai;
using UglyToad.PdfPig.Writer;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using System.Runtime.InteropServices.WindowsRuntime;

internal static class BrainDocumentTests
{
    private static void Check(bool value,string name){if(!value)throw new Exception("FAIL: "+name);Console.WriteLine("PASS: "+name);}
    public static async Task RunAsync(bool ocr)
    {
        string root=Path.Combine(Path.GetTempPath(),"astra-brain-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try
        {
            var ai=Path.Combine(root,"ai");Directory.CreateDirectory(ai);
            string runtime=Path.Combine(ai,"llama-server.exe");
            byte[] fixtureBytes="ASTRA integrity fixture, never executed"u8.ToArray();
            string hash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(fixtureBytes));
            await File.WriteAllTextAsync(Path.Combine(ai,"manifest.json"),System.Text.Json.JsonSerializer.Serialize(new{files=new Dictionary<string,string>{{"llama-server.exe",hash}}}));
            try{await BundledAi.VerifyDirectoryAsync(ai,CancellationToken.None);throw new Exception("Missing runtime accepted");}
            catch(FileNotFoundException ex){Check(ex.Message.Contains("antivirus")&&ex.Message.Contains("llama-server.exe"),"Missing AI runtime gives an actionable integrity message");}
            await File.WriteAllBytesAsync(runtime,fixtureBytes);await BundledAi.VerifyDirectoryAsync(ai,CancellationToken.None);
            Check(true,"Bundled AI verifies exact manifest bytes without executing them");
            await File.WriteAllTextAsync(runtime,"modified fixture");
            try{await BundledAi.VerifyDirectoryAsync(ai,CancellationToken.None);throw new Exception("Modified runtime accepted");}
            catch(InvalidDataException){Check(true,"Bundled AI rejects a changed component");}
            var store=new EncryptedDocumentStore(Path.Combine(root,"encrypted"));BrainDocument document;
            using(var import=store.Begin("Long reference.pdf",1201,new string('A',64)))
            {for(int i=1;i<=1201;i++)import.Add(i,$"Page {i}. Engineering study material. "+(i==1201?"orbitalmarker 73042":"general concepts"));document=import.Complete(0);}
            var found=store.Search("orbitalmarker",document.Id);
            Check(found.Count==1&&found[0].Title.Contains("1201")&&found[0].Text.Contains("73042"),"Retrieval reaches page 1201 beyond the notes limit");
            Check(store.List().Single().Pages==1201,"Page counts survive encrypted persistence");
            Check(!Directory.EnumerateFiles(Path.Combine(root,"encrypted",document.Id)).Any(p=>System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(p)).Contains("orbitalmarker")),"Document contents are not plaintext on disk");
            bool coveredLast=false;int calls=0;
            string summary=await DocumentSummarizer.SummarizeAsync(store,document,(instruction,input,ct)=>{Check(input.Length<=12000,"Summary request is bounded");coveredLast|=input.Contains("orbitalmarker");calls++;return Task.FromResult("Partial summary with original page references.");});
            Check(coveredLast&&calls>2&&summary.Length>0,"Hierarchical summary processes the final page, not only the beginning");
            using(var aborted=store.Begin("Cancelled.pdf",2,new string('B',64)))aborted.Add(1,"partial");
            Check(store.List().Count==1&&!Directory.EnumerateDirectories(Path.Combine(root,"encrypted")).Any(p=>Path.GetFileName(p).StartsWith(".import-")),"Interrupted imports leave existing documents intact");
            try{store.ReadPages("../../escape").ToArray();throw new Exception("Path traversal accepted");}catch(ArgumentException){Console.WriteLine("PASS: Document IDs reject path traversal");}
            using(var builder=new PdfDocumentBuilder())
            {
                var font=builder.AddStandard14Font(Standard14Font.Helvetica);
                for(int i=1;i<=121;i++)builder.AddPage(PageSize.A4).AddText($"Page {i}. "+(i==121?"tailpdfmarker 89123":"Ordinary study text."),14,new PdfPoint(50,740),font);
                string path=Path.Combine(root,"real-long.pdf");await File.WriteAllBytesAsync(path,builder.Build());
                var imported=await BrainPdfReader.ImportAsync(path,store);
                Check(imported.Pages==121&&store.Search("tailpdfmarker",imported.Id).Single().Text.Contains("89123"),"Real 121-page PDF is imported beyond the previous 50-page cap");
                Check((await BrainPdfReader.ImportAsync(path,store)).Id==imported.Id,"Importing the same PDF reuses its encrypted index");
            }
            if(ocr)
            {
                string textPath=Path.Combine(root,"ocr-source.pdf");
                using(var builder=new PdfDocumentBuilder()){var font=builder.AddStandard14Font(Standard14Font.Helvetica);builder.AddPage(PageSize.A4).AddText("ORBITAL OCR 73042",38,new PdfPoint(50,700),font);await File.WriteAllBytesAsync(textPath,builder.Build());}
                var pdf=await Windows.Data.Pdf.PdfDocument.LoadFromFileAsync(await Windows.Storage.StorageFile.GetFileFromPathAsync(textPath));
                using var page=pdf.GetPage(0);using var image=new Windows.Storage.Streams.InMemoryRandomAccessStream();
                await page.RenderToStreamAsync(image,new Windows.Data.Pdf.PdfPageRenderOptions{DestinationWidth=1400});image.Seek(0);using var png=new MemoryStream();await image.AsStreamForRead().CopyToAsync(png);
                string scanPath=Path.Combine(root,"scanned.pdf");
                using(var builder=new PdfDocumentBuilder()){builder.AddPage(PageSize.A4).AddPng(png.ToArray(),new PdfRectangle(0,0,595,842));await File.WriteAllBytesAsync(scanPath,builder.Build());}
                var scan=await BrainPdfReader.ImportAsync(scanPath,store);
                Check(scan.OcrPages==1&&store.Search("ORBITAL",scan.Id).Count>0,"Scanned PDF page is rendered and read with local Windows OCR");
            }
            Check(BrainCommands.Parse("apri Chrome")?.Kind=="chrome"&&BrainCommands.Parse("focus 25 minuti")?.Argument=="25","Second Brain understands useful offline commands");
            Check(BrainCommands.Parse("apri chrome; Remove-Item C:\\") is null&&BrainCommands.Parse("open https://example.com") is null,"Command parsing does not execute arbitrary scripts or URLs");
            Check(BrainCommands.Parse("aggiungi attività studiare orbite")?.Argument=="studiare orbite","Tasks preserve the user's requested text");
        }
        finally{if(Directory.Exists(root)&&Path.GetDirectoryName(root)==Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar))Directory.Delete(root,true);}
    }
}
