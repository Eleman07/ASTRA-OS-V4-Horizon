using Microsoft.UI.Xaml.Media;

namespace AstraOS.Desktop;

internal static class DesktopIconService
{
    private static readonly Dictionary<string, ImageSource> Cache = new(StringComparer.Ordinal);
    public static async Task<ImageSource?> GetAsync(ExplorerEntry entry)
    {
        string key = entry.Id + entry.LastModified.Ticks;
        if (Cache.TryGetValue(key, out var cached)) return cached;
        try
        {
            var pixels=await Task.Run(()=>NativeShellIcon.ForFile(entry.FullPath,entry.IsDirectory,64));
            var image=ShellIconBitmap.Create(pixels);if(image is null)return null;
            if (Cache.Count >= 500) Cache.Clear();
            Cache[key] = image;
            return image;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Runtime.InteropServices.COMException)
        { return null; }
    }
}
