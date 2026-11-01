using System.Text.RegularExpressions;

namespace AstraOS.Core;

/// <summary>The five editable ASTRA color tokens. Validation is UI-independent and testable on every OS.</summary>
public sealed class AstraPalette
{
    public const string DefaultNavy = "#0A1931";
    public const string DefaultIce = "#B3CFE5";
    public const string DefaultBlue = "#4A7FA7";
    public const string DefaultDeep = "#1A3D63";
    public const string DefaultSnow = "#F6FAFD";
    public string Navy { get; set; } = DefaultNavy;
    public string Ice { get; set; } = DefaultIce;
    public string Blue { get; set; } = DefaultBlue;
    public string Deep { get; set; } = DefaultDeep;
    public string Snow { get; set; } = DefaultSnow;

    public static AstraPalette Default => new();
    public bool IsDefault => Navy == DefaultNavy && Ice == DefaultIce && Blue == DefaultBlue && Deep == DefaultDeep && Snow == DefaultSnow;

    public static bool TryParse(string? value, out string normalized)
    {
        normalized = "";
        if (value is null || !Regex.IsMatch(value, "^#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant)) return false;
        normalized = value.ToUpperInvariant();
        return true;
    }

    public static bool TryCreate(string? navy, string? ice, string? blue, string? deep, string? snow, out AstraPalette palette)
    {
        palette = Default;
        if (!TryParse(navy, out var n) || !TryParse(ice, out var i) || !TryParse(blue, out var b) ||
            !TryParse(deep, out var d) || !TryParse(snow, out var s)) return false;
        palette = new AstraPalette { Navy = n, Ice = i, Blue = b, Deep = d, Snow = s };
        return true;
    }

    public static AstraPalette Sanitize(AstraPalette? palette)
        => palette is not null && TryCreate(palette.Navy, palette.Ice, palette.Blue, palette.Deep, palette.Snow, out var result)
            ? result : Default;

    public static string ContrastingText(string hex)
    {
        if (!TryParse(hex, out var color)) throw new ArgumentException("Invalid theme token", nameof(hex));
        double[] channels = new double[3];
        for (int i = 0; i < 3; i++)
        {
            var v = Convert.ToInt32(color.Substring(1 + 2 * i, 2), 16) / 255.0;
            channels[i] = v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        }
        var lum = .2126 * channels[0] + .7152 * channels[1] + .0722 * channels[2];
        // Text is always contrasted against the selected background; prefer ASTRA navy
        // for light backgrounds, falling back to black when necessary for WCAG 4.5:1.
        const double navyLuminance = 0.0102;
        if (lum > 0.179)
            return (lum + .05) / (navyLuminance + .05) >= 4.5 ? DefaultNavy : "#000000";
        return "#FFFFFF";
    }
}

public sealed class AstraTaskItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Text { get; set; } = "";
    public DateTimeOffset? CompletedUtc { get; set; }
    public bool Completed => CompletedUtc.HasValue;
}
