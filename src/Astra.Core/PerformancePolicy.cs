namespace AstraOS.Core;

/// <summary>Pure policy: budget expensive features without modifying Windows scheduling/security.</summary>
public static class PerformancePolicy
{
    public static bool IsConstrained(long ramMb,int processors)=>(ramMb>0&&ramMb<=8192)||processors<=4;
    public static int InferenceThreads(long ramMb,int processors)=>IsConstrained(ramMb,processors)?Math.Clamp(processors/2,1,2):Math.Clamp(processors/2,1,6);
    public static int NormalizeFps(int fps) => fps is 15 or 30 or 60 ? fps : 30;
    public static int EffectiveOrbitFps(AstraPreferences settings)
        => settings.ReducedMotion ? 15 : settings.EcoMode ? Math.Min(30, NormalizeFps(settings.OrbitFpsCap))
            : NormalizeFps(settings.OrbitFpsCap);
    public static bool AllowsBackgroundInference => false;
}
