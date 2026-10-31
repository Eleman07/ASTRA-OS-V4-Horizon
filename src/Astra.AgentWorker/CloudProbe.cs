using System.Text.Json;
using AstraOS.Core;
using AstraOS.Core.Agents;
using AstraOS.Desktop.Ai;

internal static class CloudProbe
{
    public static async Task RunAsync(AgentJobStore store,bool media,CancellationToken ct)
    {
        var results=new List<object>();
        if(media)
        {
            foreach(var spec in new[]{("image","ASTRA_MEDIA_IMAGE","png","Read the calibration code in the image and describe the triangle color. Do not infer hidden information."),("audio","ASTRA_MEDIA_AUDIO","wav","Transcribe the spoken calibration sentence exactly. Return only the transcript.")})
            {
                string? path=Environment.GetEnvironmentVariable(spec.Item2);if(string.IsNullOrWhiteSpace(path))continue;
                try{string answer=await LocalLlmSession.AnalyzeCloudMediaAsync(await File.ReadAllBytesAsync(path,ct),spec.Item1,spec.Item3,spec.Item4,ct);string normalized=answer.ToLowerInvariant().Replace("nine","9").Replace("five","5").Replace("seven","7").Replace("three","3").Replace("one","1");normalized=new string(normalized.Where(char.IsDigit).ToArray());results.Add(new{modality=spec.Item1,passed=normalized.Contains("95731"),answer});}
                catch(Exception ex){results.Add(new{modality=spec.Item1,passed=false,error=ex.Message});}
            }
        }
        else
        {
            string key=new OpenRouterCredentialStore(Environment.GetEnvironmentVariable("ASTRA_CREDENTIAL_DIRECTORY")).Load()??throw new InvalidOperationException("OpenRouter credential not found in the current Windows account.");
            foreach(string model in new[]{"nvidia/nemotron-3-ultra-550b-a55b:free","thinkingmachines/inkling:free","cohere/north-mini-code:free"})
            {
                try
                {
                    using var session=new LocalLlmSession();session.StartOpenRouter(key,model);
                    string task=model.Contains("ultra")
                        ?"Original deterministic source [tool-reference]: circular orbit at r=7000 km and mu=398600.4418 km^3/s^2 has period 5828.516637686015 s, or 97.14194396143358 min. Source [note-study]: orbital velocity is perpendicular to the radius in a circular two-body orbit. Deliver a study/research pack with exactly two flashcards, one worked exam exercise, a Mermaid concept map, a short evidence table and a research limitation. Preserve the period to four decimal places. Cite the actual source IDs. Also add a gaming experiment rule that states FPS is unmeasured, and a creator hook for a proposed CubeSat with no claimed flight results. Keep this synthetic smoke response concise."
                        :"Write a compact Python function circular_period(radius_km,mu_km3_s2) with finite-positive validation and two assertions for the 7000 km Earth case (expected 5828.516637686015 seconds) and invalid input. Explain assumptions briefly. This is a synthetic availability smoke check, not a request to execute code.";
                    string answer=await session.RunTaskAsync("You are ASTRA. Deliver the requested artifacts using only the supplied records; label proposals and unknowns. Answer in English.",task,false,0,ct);
                    bool passed=model.Contains("ultra")?answer.Contains("5828.5166")&&answer.Contains("97.1419")&&answer.Contains("tool-reference")&&answer.Contains("mermaid",StringComparison.OrdinalIgnoreCase):answer.Contains("def ")&&answer.Contains("assert")&&answer.Contains("5828");
                    results.Add(new{requested=model,effective=session.EffectiveModel,notice=session.LastModelNotice,passed,truncated=session.LastTaskTruncated,answer});
                }
                catch(Exception ex){results.Add(new{requested=model,passed=false,error=ex.Message});}
            }
        }
        File.WriteAllText(Path.Combine(store.Root,media?"online-media-probe.json":"online-quality-probe.json"),JsonSerializer.Serialize(new{syntheticFixtures=true,localInference=false,results},new JsonSerializerOptions{WriteIndented=true}));
    }
}
