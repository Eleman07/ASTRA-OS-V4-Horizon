namespace AstraOS.Core;

public sealed record RenderBudget(int GraphNodes, int FramesPerSecond, double PixelRatio, string Quality)
{
    public static RenderBudget Select(long memoryMb, int processors, bool eco, bool reducedMotion)
    {
        bool constrained = PerformancePolicy.IsConstrained(memoryMb, processors);
        if (constrained || eco) return new(120, reducedMotion ? 0 : 15, 1, "low");
        return memoryMb >= 24000 && processors >= 8
            ? new(600, reducedMotion ? 0 : 30, 1.5, "high")
            : new(300, reducedMotion ? 0 : 24, 1.25, "balanced");
    }
}
