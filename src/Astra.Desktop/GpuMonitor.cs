using System.Runtime.InteropServices;

namespace AstraOS.Desktop;

/// <summary>Optional Windows GPU Engine performance counters; null when unsupported.
/// Reports the busiest physical engine, summing its process instances, rather than
/// adding unrelated engines together. This is a sampled utilization estimate.</summary>
internal sealed class GpuMonitor : IDisposable
{
    private nint _query, _counter;
    private bool _attempted;
    public double? Capture()
    {
        if (!_attempted)
        {
            _attempted = true;
            if (PdhOpenQueryW(null, 0, out _query) != 0 ||
                PdhAddEnglishCounterW(_query, @"\GPU Engine(*)\Utilization Percentage", 0, out _counter) != 0)
            { Dispose(); return null; }
            PdhCollectQueryData(_query); return null;
        }
        if (_query == 0 || PdhCollectQueryData(_query) != 0) return null;
        uint bytes = 0;
        if (PdhGetFormattedCounterArrayW(_counter, 0x200, ref bytes, out _, 0) != 0x800007D2 || bytes is 0 or > 8_000_000) return null;
        var buffer = Marshal.AllocHGlobal((int)bytes);
        try
        {
            if (PdhGetFormattedCounterArrayW(_counter, 0x200, ref bytes, out var count, buffer) != 0 || count > 20000) return null;
            var engines = new Dictionary<string, double>(StringComparer.Ordinal);
            var size = Marshal.SizeOf<CounterItem>();
            if ((ulong)count * (ulong)size > bytes) return null;
            for (int i = 0; i < count; i++)
            {
                var item = Marshal.PtrToStructure<CounterItem>(buffer + i * size);
                if (item.Value.Status > 1 || !double.IsFinite(item.Value.Number)) continue;
                var name = Marshal.PtrToStringUni(item.Name) ?? "";
                int keyIndex = name.IndexOf("luid_", StringComparison.Ordinal);
                if (keyIndex < 0) continue;
                var key = name[keyIndex..];
                engines[key] = engines.GetValueOrDefault(key) + Math.Max(0, item.Value.Number);
            }
            return engines.Count == 0 ? null : Math.Clamp(engines.Values.Max(), 0, 100);
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }
    public void Dispose()
    {
        if (_query != 0) PdhCloseQuery(_query);
        _query = _counter = 0;
    }
    [StructLayout(LayoutKind.Sequential)] private struct CounterValue { public uint Status; public double Number; }
    [StructLayout(LayoutKind.Sequential)] private struct CounterItem { public nint Name; public CounterValue Value; }
    [DllImport("pdh.dll", CharSet = CharSet.Unicode)] private static extern uint PdhOpenQueryW(string? source, nuint userData, out nint query);
    [DllImport("pdh.dll", CharSet = CharSet.Unicode)] private static extern uint PdhAddEnglishCounterW(nint query, string path, nuint userData, out nint counter);
    [DllImport("pdh.dll")] private static extern uint PdhCollectQueryData(nint query);
    [DllImport("pdh.dll", CharSet = CharSet.Unicode)] private static extern uint PdhGetFormattedCounterArrayW(nint counter, uint format, ref uint bytes, out uint count, nint buffer);
    [DllImport("pdh.dll")] private static extern uint PdhCloseQuery(nint query);
}
