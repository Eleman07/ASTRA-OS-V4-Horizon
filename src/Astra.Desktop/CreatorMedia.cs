using System.Text.Json;
using System.Security.Cryptography;
using AstraOS.Core;
using Windows.Graphics.Imaging;
using Windows.Storage;
namespace AstraOS.Desktop;
public sealed record CreativeAsset(string Id,string Path,string Name,string Kind,uint Width,uint Height,double Seconds,ulong Bytes,string[] Colors,string Note="",double? SourceSeconds=null,string AltText="",string Rights="");
internal static class CreatorMedia
{
    public static readonly string[] Extensions={".png",".jpg",".jpeg",".bmp",".gif",".webp",".tif",".mp4",".mov",".mkv",".avi",".wmv",".wav",".mp3",".m4a",".flac"};
    public static async Task<CreativeAsset> ReadAsync(string path)
    {
        var file=await StorageFile.GetFileFromPathAsync(path);var basic=await file.GetBasicPropertiesAsync();uint width=0,height=0;double seconds=5;string kind="audio";string[] colors=Array.Empty<string>();
        if(new[]{".png",".jpg",".jpeg",".bmp",".gif",".webp",".tif"}.Contains(file.FileType.ToLowerInvariant()))
        {
            kind="image";if(basic.Size>32_000_000)throw new InvalidDataException(AstraOS.Core.UiText.T("Immagine oltre 32 MB."));using var stream=await file.OpenReadAsync();var decoder=await BitmapDecoder.CreateAsync(stream);width=decoder.PixelWidth;height=decoder.PixelHeight;
            double scale=Math.Min(1,120.0/Math.Max(width,height));var data=await decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Straight,new BitmapTransform{ScaledWidth=(uint)Math.Max(1,width*scale),ScaledHeight=(uint)Math.Max(1,height*scale)},ExifOrientationMode.RespectExifOrientation,ColorManagementMode.ColorManageToSRgb);byte[] pixels=data.DetachPixelData();var bins=new Dictionary<int,(int Count,long R,long G,long B)>();
            for(int i=0;i+3<pixels.Length;i+=4){if(pixels[i+3]<128)continue;int b=pixels[i],g=pixels[i+1],r=pixels[i+2],key=(r>>4)*256+(g>>4)*16+(b>>4);var v=bins.GetValueOrDefault(key);bins[key]=(v.Count+1,v.R+r,v.G+g,v.B+b);}
            var chosen=new List<(int R,int G,int B)>();foreach(var bin in bins.Values.OrderByDescending(v=>v.Count)){var c=((int)(bin.R/bin.Count),(int)(bin.G/bin.Count),(int)(bin.B/bin.Count));if(chosen.All(p=>Math.Abs(p.R-c.Item1)+Math.Abs(p.G-c.Item2)+Math.Abs(p.B-c.Item3)>65))chosen.Add(c);if(chosen.Count==6)break;}colors=chosen.Select(c=>$"#{c.R:X2}{c.G:X2}{c.B:X2}").ToArray();
        }
        else if(new[]{".mp4",".mov",".mkv",".avi",".wmv"}.Contains(file.FileType.ToLowerInvariant())){kind="video";var props=await file.Properties.GetVideoPropertiesAsync();width=props.Width;height=props.Height;seconds=props.Duration.TotalSeconds;}
        else{var props=await file.Properties.GetMusicPropertiesAsync();seconds=props.Duration.TotalSeconds;}
        return new(Guid.NewGuid().ToString("N"),path,file.Name,kind,width,height,seconds>0?seconds:5,basic.Size,colors,SourceSeconds:kind!="image"&&seconds>0?seconds:null);
    }
    private static string BoardPath=>System.IO.Path.Combine(AstraPaths.UserDataRoot,"creator","board.dpapi");
    public static List<CreativeAsset> Load()
    {
        if(!File.Exists(BoardPath))return new();if(new FileInfo(BoardPath).Length>2_000_000)throw new InvalidDataException(AstraOS.Core.UiText.T("Bacheca troppo grande."));var bytes=ProtectedData.Unprotect(File.ReadAllBytes(BoardPath),null,DataProtectionScope.CurrentUser);
        try{var assets=JsonSerializer.Deserialize<List<CreativeAsset>>(bytes)??new();if(assets.Count>32)throw new InvalidDataException(AstraOS.Core.UiText.T("Massimo 32 asset."));return assets;}finally{CryptographicOperations.ZeroMemory(bytes);}
    }
    public static void Save(IReadOnlyList<CreativeAsset> assets)
    {
        if(assets.Count>32)throw new InvalidDataException(AstraOS.Core.UiText.T("Massimo 32 asset."));Directory.CreateDirectory(System.IO.Path.GetDirectoryName(BoardPath)!);var plain=JsonSerializer.SerializeToUtf8Bytes(assets);try{string tmp=BoardPath+".tmp";File.WriteAllBytes(tmp,ProtectedData.Protect(plain,null,DataProtectionScope.CurrentUser));File.Move(tmp,BoardPath,true);}finally{CryptographicOperations.ZeroMemory(plain);}
    }
}
