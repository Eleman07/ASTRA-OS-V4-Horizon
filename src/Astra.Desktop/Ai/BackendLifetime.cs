using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
namespace AstraOS.Desktop.Ai;

internal static class BackendLifetime
{
    [StructLayout(LayoutKind.Sequential)]private struct Limits{public long ProcessTime,JobTime;public uint Flags;public UIntPtr MinWorkingSet,MaxWorkingSet;public uint ActiveProcesses;public UIntPtr Affinity;public uint Priority,Scheduling;}
    [StructLayout(LayoutKind.Sequential)]private struct Io{public ulong ReadOps,WriteOps,OtherOps,ReadBytes,WriteBytes,OtherBytes;}
    [StructLayout(LayoutKind.Sequential)]private struct Extended{public Limits Basic;public Io Io;public UIntPtr ProcessMemory,JobMemory,PeakProcessMemory,PeakJobMemory;}
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]private static extern SafeFileHandle CreateJobObjectW(nint attributes,string? name);
    [DllImport("kernel32.dll",SetLastError=true)]private static extern bool SetInformationJobObject(SafeFileHandle job,int info,ref Extended limits,uint size);
    [DllImport("kernel32.dll",SetLastError=true)]private static extern bool AssignProcessToJobObject(SafeFileHandle job,nint process);
    public static IDisposable? Attach(Process process)
    {
        var handle=CreateJobObjectW(0,null);var limits=new Extended{Basic=new Limits{Flags=0x2000}};
        if(handle.IsInvalid||!SetInformationJobObject(handle,9,ref limits,(uint)Marshal.SizeOf<Extended>())||!AssignProcessToJobObject(handle,process.Handle)){handle.Dispose();return null;}
        return handle;
    }
}
