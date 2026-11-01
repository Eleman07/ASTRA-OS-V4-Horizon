using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace AstraOS.Core;

/// <summary>Quote complete untrusted prompt data as JSON without expanding ordinary non-ASCII text into escape sequences.</summary>
public static class PromptData
{
    private static readonly JsonSerializerOptions Options=new(){Encoder=JavaScriptEncoder.Create(UnicodeRanges.All)};
    public static string Quote(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return JsonSerializer.Serialize(text,Options);
    }
}
