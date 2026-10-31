using System.Runtime.InteropServices;

namespace AstraOS.Desktop;

internal sealed record IconPixels(int Size,byte[] Bgra)
{
    public bool HasPixels=>Enumerable.Range(0,Size*Size).Any(i=>Bgra[i*4+3]>0);
}

// Read actual Shell/window icons rather than thumbnail streams. HICONs are drawn
// onto black and white DIBs to preserve both modern alpha and legacy icon masks.
internal static class NativeShellIcon
{
    [DllImport("ole32.dll")]private static extern int CoInitializeEx(nint reserved,uint flags);
    [DllImport("ole32.dll")]private static extern void CoUninitialize();
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]private struct ShellInfo
    {public nint Icon;public int Index;public uint Attributes;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=260)]public string DisplayName;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=80)]public string TypeName;}
    [StructLayout(LayoutKind.Sequential)]private struct BitmapInfo
    {public uint HeaderSize;public int Width,Height;public ushort Planes,Bits;public uint Compression,ImageSize;public int XPixels,YPixels;public uint Used,Important,Colors;}
    [DllImport("shell32.dll",CharSet=CharSet.Unicode)]private static extern nuint SHGetFileInfo(string path,uint attributes,out ShellInfo info,uint size,uint flags);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern nint SendMessageTimeout(nint window,uint message,nuint wParam,nint lParam,uint flags,uint timeout,out nint result);
    [DllImport("user32.dll",EntryPoint="GetClassLongPtrW")]private static extern nint GetClassLongPtr(nint window,int index);
    [DllImport("user32.dll")]private static extern nint CopyIcon(nint icon);
    [DllImport("user32.dll")]private static extern bool DestroyIcon(nint icon);
    [DllImport("user32.dll",EntryPoint="LoadIconW")]private static extern nint LoadIcon(nint instance,nint id);
    [DllImport("user32.dll")]private static extern bool DrawIconEx(nint dc,int x,int y,nint icon,int width,int height,uint step,nint brush,uint flags);
    [DllImport("gdi32.dll")]private static extern nint CreateCompatibleDC(nint dc);
    [DllImport("gdi32.dll")]private static extern bool DeleteDC(nint dc);
    [DllImport("gdi32.dll")]private static extern nint CreateDIBSection(nint dc,in BitmapInfo info,uint usage,out nint pixels,nint section,uint offset);
    [DllImport("gdi32.dll")]private static extern nint SelectObject(nint dc,nint item);
    [DllImport("gdi32.dll")]private static extern bool DeleteObject(nint item);
    [DllImport("gdi32.dll")]private static extern bool GdiFlush();

    public static IconPixels? ForFile(string path,bool directory=false,int size=64)
    {
        size=Math.Clamp(size,16,256);nint icon=0;int apartment=CoInitializeEx(0,0);
        try
        {
            var result=SHGetFileInfo(path,0,out var info,(uint)Marshal.SizeOf<ShellInfo>(),0x100|0x20);
            if(result!=0)icon=info.Icon;
            if(icon==0){SHGetFileInfo(directory?"folder":"file"+Path.GetExtension(path),directory?0x10u:0x80u,out info,(uint)Marshal.SizeOf<ShellInfo>(),0x100|0x10);icon=info.Icon;}
            return icon!=0?Draw(icon,size):Default(size);
        }
        finally{if(icon!=0)DestroyIcon(icon);if(apartment>=0)CoUninitialize();}
    }
    public static IconPixels? ForWindow(long handle,string executable,int size=48)
    {
        nint owned=0;size=Math.Clamp(size,16,256);
        try
        {
            foreach(nuint kind in new nuint[]{1,2,0})
            {if(SendMessageTimeout((nint)handle,0x7F,kind,0,3,70,out var icon)!=0&&icon!=0){owned=CopyIcon(icon);if(owned!=0)break;}}
            if(owned==0){var icon=GetClassLongPtr((nint)handle,-14);if(icon==0)icon=GetClassLongPtr((nint)handle,-34);if(icon!=0)owned=CopyIcon(icon);}
            var pixels=owned!=0?Draw(owned,size):null;
            return pixels?.HasPixels==true?pixels:!string.IsNullOrWhiteSpace(executable)?ForFile(executable,false,size):Default(size);
        }
        finally{if(owned!=0)DestroyIcon(owned);}
    }
    private static IconPixels? Default(int size)=>Draw(LoadIcon(0,(nint)32512),size);
    private static IconPixels? Draw(nint icon,int size)
    {
        if(icon==0)return null;
        nint dc=CreateCompatibleDC(0),dib=0,previous=0;
        try
        {
            if(dc==0)return null;
            var info=new BitmapInfo{HeaderSize=40,Width=size,Height=-size,Planes=1,Bits=32};
            dib=CreateDIBSection(dc,in info,0,out var address,0,0);if(dib==0||address==0)return null;
            previous=SelectObject(dc,dib);int length=size*size*4;
            var black=new byte[length];for(int i=3;i<length;i+=4)black[i]=255;
            Marshal.Copy(black,0,address,length);if(!DrawIconEx(dc,0,0,icon,size,size,0,0,3))return null;GdiFlush();Marshal.Copy(address,black,0,length);
            var white=new byte[length];Array.Fill(white,(byte)255);Marshal.Copy(white,0,address,length);if(!DrawIconEx(dc,0,0,icon,size,size,0,0,3))return null;GdiFlush();Marshal.Copy(address,white,0,length);
            for(int i=0;i<length;i+=4)
            {
                int alpha=255-Math.Max(white[i]-black[i],Math.Max(white[i+1]-black[i+1],white[i+2]-black[i+2]));
                black[i+3]=(byte)Math.Clamp(alpha,0,255);
                for(int channel=0;channel<3;channel++)black[i+channel]=Math.Min(black[i+channel],black[i+3]);
            }
            var result=new IconPixels(size,black);return result.HasPixels?result:null;
        }
        finally{if(previous!=0)SelectObject(dc,previous);if(dib!=0)DeleteObject(dib);if(dc!=0)DeleteDC(dc);}
    }
}
