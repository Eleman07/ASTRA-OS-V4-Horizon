using System.Text.Json;

if (args.Length != 1 || args[0] != "--astra-plugin-v2") return 2;
try
{
    string? line = await Console.In.ReadLineAsync();
    if (line is null || line.Length > 16_384) return 3;
    using var document = JsonDocument.Parse(line);
    var obj = document.RootElement;
    if (obj.GetProperty("apiVersion").GetInt32() != 2 || obj.GetProperty("operation").GetString() != "transform-text") return 4;
    string input = obj.GetProperty("text").GetString() ?? "";
    Console.Write(JsonSerializer.Serialize(new { text = $"[ASTRA SAMPLE] {input.Trim()}" }));
    return 0;
}
catch (Exception) { return 5; }
