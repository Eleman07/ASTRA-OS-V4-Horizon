using System.Security.Cryptography;
using AstraOS.Core.SecondBrain;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;
using Windows.Storage.Streams;

namespace AstraOS.Desktop.Ai;
public static class BrainPdfReader
{
    public const long MaxFileBytes=512L*1024*1024;
    public static Task<BrainDocument> ImportAsync(string path,EncryptedDocumentStore store,IProgress<(int Page,int Total)>? progress=null,CancellationToken ct=default)
        =>Task.Run(async()=>
        {
            var info=new FileInfo(path);
            if(!info.Exists||!info.Extension.Equals(".pdf",StringComparison.OrdinalIgnoreCase)||info.Length>MaxFileBytes)throw new InvalidDataException("Select a PDF no larger than 512 MB.");
            using var source=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
            string hash=Convert.ToHexString(await SHA256.HashDataAsync(source,ct));source.Position=0;
            var existing=store.List().FirstOrDefault(d=>d.Sha256==hash);if(existing is not null)return existing;
            using var pdf=UglyToad.PdfPig.PdfDocument.Open(source);
            if(pdf.NumberOfPages>EncryptedDocumentStore.MaxPages)throw new InvalidDataException("PDF exceeds 10,000 pages.");
            using var import=store.Begin(info.Name,pdf.NumberOfPages,hash);
            Windows.Data.Pdf.PdfDocument? rendered=null;OcrEngine? ocr=null;int ocrPages=0;
            for(int number=1;number<=pdf.NumberOfPages;number++)
            {
                ct.ThrowIfCancellationRequested();string text=ContentOrderTextExtractor.GetText(pdf.GetPage(number));
                if(string.IsNullOrWhiteSpace(text))
                {
                    ocr??=OcrEngine.TryCreateFromUserProfileLanguages()??OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language("en-US"));
                    if(ocr is null)throw new InvalidOperationException("Install a Windows OCR language to read scanned PDF pages.");
                    rendered??=await Windows.Data.Pdf.PdfDocument.LoadFromFileAsync(await StorageFile.GetFileFromPathAsync(info.FullName)).AsTask(ct);
                    using var page=rendered.GetPage((uint)(number-1));using var stream=new InMemoryRandomAccessStream();
                    double scale=Math.Min(2,1800/Math.Max(page.Size.Width,page.Size.Height));
                    await page.RenderToStreamAsync(stream,new Windows.Data.Pdf.PdfPageRenderOptions{DestinationWidth=(uint)Math.Max(1,page.Size.Width*scale),DestinationHeight=(uint)Math.Max(1,page.Size.Height*scale)}).AsTask(ct);
                    stream.Seek(0);var decoder=await BitmapDecoder.CreateAsync(stream).AsTask(ct);using var bitmap=await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Ignore).AsTask(ct);
                    text=(await ocr.RecognizeAsync(bitmap).AsTask(ct)).Text;ocrPages++;
                }
                import.Add(number,text,ct);progress?.Report((number,pdf.NumberOfPages));
            }
            ct.ThrowIfCancellationRequested();return import.Complete(ocrPages);
        },ct);
}
