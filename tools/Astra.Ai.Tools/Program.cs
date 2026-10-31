using System.Text.Json;
using System.Text.RegularExpressions;
using AstraOS.Desktop.Ai;

var root=Path.GetFullPath(args[0]);Environment.SetEnvironmentVariable("ASTRA_DATA_DIRECTORY",Path.Combine(root,"docs","evidence","qa-ai-tools"));
using var session=new LocalLlmSession();using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(60));
await session.StartAsync(Path.Combine(root,"src","Astra.Desktop","Ai","components","llama-server.exe"),Path.Combine(root,"src","Astra.Desktop","Ai","components","astra-local-model.gguf"),false,timeout.Token);
if(args.Length<2||args[1]=="probe")
{
    string answer=await session.RunTaskAsync("Answer in Italian in two short bullet points.","Explain why a finite wing has induced drag.",false,200,timeout.Token);Console.WriteLine("AI_LOCAL_RESPONSE: "+answer);return;
}
string language=args[2],input=args[3],output=args[4];var sources=JsonSerializer.Deserialize<List<string>>(File.ReadAllText(input))!;
var translated=File.Exists(output)?JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(output))!:new Dictionary<string,string>();
string fragmentFile=output+".fragments";var fragments=File.Exists(fragmentFile)?JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(fragmentFile))!:new Dictionary<string,string>();
string[] Parts(string s)=>Regex.Split(s,@"(\{\d+[^{}]*\})");
var pending=sources.Where(s=>!translated.ContainsKey(s)).SelectMany(Parts).Select(p=>p.Trim()).Where(p=>p.Length>1&&p.Any(char.IsLetter)&&!p.StartsWith('{')).Distinct().Where(p=>!fragments.ContainsKey(p)).ToArray();
foreach(var group in pending.Chunk(6))
{
    bool valid=false;
    for(int retry=0;retry<3&&!valid;retry++)
    {
        try
        {
            string prompt="Translate all input strings from Italian to "+language+". Return only the JSON object with the t array, in identical input order. Preserve brands and units. Do not add explanations. Exactly "+group.Length+" strings.";
            using var schema=JsonDocument.Parse("{\"type\":\"object\",\"properties\":{\"t\":{\"type\":\"array\",\"items\":{\"type\":\"string\"},\"minItems\":"+group.Length+",\"maxItems\":"+group.Length+"}},\"required\":[\"t\"],\"additionalProperties\":false}");
            string response=await session.RunTaskAsync(prompt,JsonSerializer.Serialize(group),true,3800,timeout.Token,schema.RootElement,language);using var doc=JsonDocument.Parse(response);var items=doc.RootElement.GetProperty("t").EnumerateArray().Select(x=>x.GetString()??"").ToArray();if(items.Length!=group.Length||items.Any(string.IsNullOrWhiteSpace))throw new InvalidDataException("Translation cardinality");
            for(int i=0;i<group.Length;i++)fragments[group[i]]=items[i].Trim();valid=true;
            File.WriteAllText(fragmentFile,JsonSerializer.Serialize(fragments));
            foreach(var source in sources){var parts=Parts(source);if(parts.Where(p=>p.Trim().Length>1&&p.Any(char.IsLetter)&&!p.StartsWith('{')).All(p=>fragments.ContainsKey(p.Trim()))){translated[source]=string.Concat(parts.Select(p=>{var key=p.Trim();if(!fragments.TryGetValue(key,out var tr))return p;return p[..(p.Length-p.TrimStart().Length)]+tr+p[(p.TrimEnd().Length)..];}));}}
            File.WriteAllText(output,JsonSerializer.Serialize(translated,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"TRANSLATED {translated.Count}/{sources.Count} · fragments {fragments.Count}");
        }
        catch(Exception ex)when(ex is JsonException or InvalidDataException){Console.WriteLine("RETRY "+ex.Message);}
    }
    if(!valid)
    {
        foreach(var source in group)
        {
            using var schema=JsonDocument.Parse("{\"type\":\"object\",\"properties\":{\"translation\":{\"type\":\"string\"}},\"required\":[\"translation\"],\"additionalProperties\":false}");
            string reply=await session.RunTaskAsync("Translate this single Italian UI fragment into "+language+". Return its translation as the JSON translation field. Preserve punctuation and units.",source,true,2000,timeout.Token,schema.RootElement,language);using var single=JsonDocument.Parse(reply);string value=single.RootElement.GetProperty("translation").GetString()??"";if(string.IsNullOrWhiteSpace(value))throw new InvalidDataException("Empty fragment: "+source);fragments[source]=value.Trim();
        }
        File.WriteAllText(fragmentFile,JsonSerializer.Serialize(fragments));
        foreach(var source in sources){var parts=Parts(source);if(parts.Where(p=>p.Trim().Length>1&&p.Any(char.IsLetter)&&!p.StartsWith('{')).All(p=>fragments.ContainsKey(p.Trim())))translated[source]=string.Concat(parts.Select(p=>fragments.TryGetValue(p.Trim(),out var value)?p[..(p.Length-p.TrimStart().Length)]+value+p[p.TrimEnd().Length..]:p));}
        File.WriteAllText(output,JsonSerializer.Serialize(translated,new JsonSerializerOptions{WriteIndented=true}));
    }
}
foreach(var source in sources)if(!translated.ContainsKey(source))translated[source]=source;
File.WriteAllText(output,JsonSerializer.Serialize(translated,new JsonSerializerOptions{WriteIndented=true}));
