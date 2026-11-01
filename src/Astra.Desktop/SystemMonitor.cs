using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace AstraOS.Desktop;

public sealed record LiveSystemSample(DateTimeOffset Time, double? CpuPercent, double? RamPercent,
    ulong TotalMemory, ulong AvailableMemory, long DiskTotal, long DiskAvailable,
    string Battery, string Network, int ProcessCount, string Os, double? GpuPercent);

/// <summary>Documented Win32 counters. Called on a worker; never invents unavailable readings.</summary>
public sealed class SystemMonitor : IDisposable
{
    private ulong _idle, _kernel, _user;
    private readonly GpuMonitor _gpu = new();
    private readonly object _gate = new();
    private bool _disposed;
    public LiveSystemSample Capture()
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return CaptureCore();
        }
    }
    private LiveSystemSample CaptureCore()
    {
        double? cpu = null;
        if (GetSystemTimes(out var idle, out var kernel, out var user))
        {
            if (_kernel != 0 && kernel >= _kernel && user >= _user && idle >= _idle)
            {
                var total = kernel - _kernel + user - _user;
                if (total > 0) cpu = Math.Clamp(100.0 * (total - Math.Min(total, idle - _idle)) / total, 0, 100);
            }
            (_idle, _kernel, _user) = (idle, kernel, user);
        }
        var memory = new MemoryStatus { Length = (uint)Marshal.SizeOf<MemoryStatus>() };
        var hasMemory = GlobalMemoryStatusEx(ref memory);
        long totalDisk = 0, availableDisk = 0;
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory)!);
            if (drive.IsReady) { totalDisk = drive.TotalSize; availableDisk = drive.AvailableFreeSpace; }
        }
        catch (IOException) { }
        string battery = "Non disponibile";
        if (GetSystemPowerStatus(out var power) && power.BatteryFlag != 128 && power.BatteryLifePercent <= 100)
            battery = $"{power.BatteryLifePercent}% · {(power.ACLineStatus == 1 ? "alimentazione collegata" : "batteria")}";
        var processes = Process.GetProcesses();
        int count = processes.Length;
        foreach (var process in processes) process.Dispose();
        string network;
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback).ToArray();
            network = interfaces.Length == 0 ? "Offline · nessuna interfaccia attiva" :
                $"{interfaces.Length} interfacce attive · accesso Internet non verificato";
        }
        catch (NetworkInformationException) { network = "Non disponibile"; }
        return new(DateTimeOffset.Now, cpu, hasMemory ? memory.MemoryLoad : null,
            hasMemory ? memory.TotalPhysical : 0, hasMemory ? memory.AvailablePhysical : 0,
            totalDisk, availableDisk, battery, network, count, Environment.OSVersion.VersionString, _gpu.Capture());
    }
    public void Dispose() { lock (_gate) { _disposed = true; _gpu.Dispose(); } }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatus
    {
        public uint Length, MemoryLoad;
        public ulong TotalPhysical, AvailablePhysical, TotalPageFile, AvailablePageFile,
            TotalVirtual, AvailableVirtual, AvailableExtendedVirtual;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct PowerStatus
    {
        public byte ACLineStatus, BatteryFlag, BatteryLifePercent, SystemStatusFlag;
        public uint BatteryLifeTime, BatteryFullLifeTime;
    }
    [DllImport("kernel32.dll")][return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out ulong idle, out ulong kernel, out ulong user);
    [DllImport("kernel32.dll")][return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatus memory);
    [DllImport("kernel32.dll")][return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemPowerStatus(out PowerStatus status);
}
