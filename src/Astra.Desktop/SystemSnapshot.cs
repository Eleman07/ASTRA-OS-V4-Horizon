using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AstraOS.Desktop;

public sealed record SystemSnapshot(
    string Os, string Architecture, long AppMemoryMb, long PhysicalMemoryMb, string CpuThreads)
{
    public static SystemSnapshot Capture()
    {
        using var process = Process.GetCurrentProcess();
        var memory = new MemoryStatus { Length = (uint)Marshal.SizeOf<MemoryStatus>() };
        var ram = GlobalMemoryStatusEx(ref memory)
            ? (long)(memory.TotalPhysical / (1024 * 1024)) : 0;
        return new SystemSnapshot(
            Environment.OSVersion.VersionString,
            RuntimeInformation.OSArchitecture.ToString(),
            process.WorkingSet64 / (1024 * 1024),
            ram,
            Environment.ProcessorCount.ToString());
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatus
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatus memory);
}
