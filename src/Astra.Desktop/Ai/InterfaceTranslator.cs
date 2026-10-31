using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using AstraOS.Core;
namespace AstraOS.Desktop.Ai;

internal static class InterfaceTranslator
{
    public static async Task CreateAsync(LocalLlmSession session,Action<int,int> progress,CancellationToken ct)
    {
        string code=CultureInfo.GetCultureInfo(UiText.Language).TwoLetterISOLanguageName;string folder=Path.Combine(AstraPaths.UserDataRoot,"languages");Directory.CreateDirectory(folder);string path=Path.Combine(folder,code+".json");
        var sources=JsonSerializer.Deserialize<string[]>(await File.ReadAllTextAsync(Path.Combine(UiText.DirectoryPath,"sources.json"),ct))??Array.Empty<string>();
        var translated=File.Exists(path)?JsonSerializer.Deserialize<Dictionary<string,string>>(await File.ReadAllTextAsync(path,ct))??new():new Dictionary<string,string>();
        string[] Parts(string text)=>Regex.Split(text,@"(\{\d+[^{}]*\})");var pieces=sources.SelectMany(Parts).Select(s=>s.Trim()).Where(s=>s.Length>1&&s.Any(char.IsLetter)&&!s.StartsWith('{')).Distinct().ToArray();string fragmentPath=path+".fragments";var fragments=File.Exists(fragmentPath)?JsonSerializer.Deserialize<Dictionary<string,string>>(await File.ReadAllTextAsync(fragmentPath,ct))??new():new Dictionary<string,string>();
        foreach(var chunk in pieces.Where(p=>!fragments.ContainsKey(p)).Chunk(6))
        {
            ct.ThrowIfCancellationRequested();using var schema=JsonDocument.Parse("{\"type\":\"object\",\"properties\":{\"t\":{\"type\":\"array\",\"items\":{\"type\":\"string\"},\"minItems\":"+chunk.Length+",\"maxItems\":"+chunk.Length+"}},\"required\":[\"t\"],\"additionalProperties\":false}");
            string[] result;
            try
            {
                string reply=await session.RunTaskAsync("Translate each Italian UI string to "+UiText.LanguageName+". Return the t array in the same order. Preserve brands, numbers and units.",JsonSerializer.Serialize(chunk),true,3800,ct,schema.RootElement);using var doc=JsonDocument.Parse(reply);result=doc.RootElement.GetProperty("t").EnumerateArray().Select(x=>x.GetString()??"").ToArray();if(result.Length!=chunk.Length||result.Any(string.IsNullOrWhiteSpace))throw new InvalidDataException(AstraOS.Core.UiText.T("Translation cardinality"));
            }
            catch(Exception ex)when(ex is JsonException or InvalidDataException)
            {
                result=new string[chunk.Length];using var singleSchema=JsonDocument.Parse("{\"type\":\"object\",\"properties\":{\"translation\":{\"type\":\"string\",\"minLength\":1}},\"required\":[\"translation\"],\"additionalProperties\":false}");
                for(int i=0;i<chunk.Length;i++){string reply=await session.RunTaskAsync("Translate this UI fragment into "+UiText.LanguageName+". Return a JSON translation field. Preserve punctuation and units.",chunk[i],true,2000,ct,singleSchema.RootElement);using var doc=JsonDocument.Parse(reply);result[i]=doc.RootElement.GetProperty("translation").GetString()??"";if(string.IsNullOrWhiteSpace(result[i]))throw new InvalidDataException(AstraOS.Core.UiText.T("Empty translation"));}
            }
            for(int i=0;i<chunk.Length;i++)fragments[chunk[i]]=result[i].Trim();
            foreach(var source in sources){var parts=Parts(source);if(parts.Where(s=>s.Trim().Length>1&&s.Any(char.IsLetter)&&!s.StartsWith('{')).All(s=>fragments.ContainsKey(s.Trim())))translated[source]=string.Concat(parts.Select(s=>fragments.TryGetValue(s.Trim(),out var value)?s[..(s.Length-s.TrimStart().Length)]+value+s[s.TrimEnd().Length..]:s));}
            await File.WriteAllTextAsync(fragmentPath,JsonSerializer.Serialize(fragments),ct);progress(translated.Count,sources.Length);await Task.Delay(30,ct);
        }
        foreach(var source in sources)translated[source]=string.Concat(Parts(source).Select(s=>fragments.TryGetValue(s.Trim(),out var value)?s[..(s.Length-s.TrimStart().Length)]+value+s[s.TrimEnd().Length..]:s));string temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";try{await File.WriteAllTextAsync(temporary,JsonSerializer.Serialize(translated),ct);File.Move(temporary,path,true);}finally{if(File.Exists(temporary))File.Delete(temporary);}
    }
}
