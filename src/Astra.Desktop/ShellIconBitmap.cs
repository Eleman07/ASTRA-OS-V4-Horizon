using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;

namespace AstraOS.Desktop;
internal static class ShellIconBitmap
{
    public static ImageSource? Create(IconPixels? pixels)
    {
        if(pixels?.HasPixels!=true)return null;
        var bitmap=new WriteableBitmap(pixels.Size,pixels.Size);
        using(var stream=bitmap.PixelBuffer.AsStream())stream.Write(pixels.Bgra);
        bitmap.Invalidate();return bitmap;
    }
}
