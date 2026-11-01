using System.Net;
using System.Text.Json;
using AstraOS.Core.Agents;

internal static class FreeCloudCompletionTests
{
    public static void Run(Action<bool, string> check)
    {
        const string primary = "nvidia/nemotron-3-ultra-550b-a55b:free", secondary = "cohere/north-mini-code:free";
        string catalog = JsonSerializer.Serialize(new { data = new[] { primary, secondary, "qwen/excluded:free", "paid/never:free" }.Select(id => new {
            id, name = id, created = 1, context_length = 1000000,
            pricing = new { prompt = id.StartsWith("paid/") ? "1" : "0", completion = "0", request = "0", image = "0" },
            architecture = new { input_modalities = new[] { "text" }, output_modalities = new[] { "text" } }, top_provider = new { max_completion_tokens = 65536 }
        }) });
        var models = new List<string>(); string longInput = new('x', 110000);
        using (var client = Client(async request => {
            if (request.Method == HttpMethod.Get) return Response(HttpStatusCode.OK, catalog);
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            string model = body.RootElement.GetProperty("model").GetString()!; models.Add(model);
            check(body.RootElement.GetProperty("messages")[0].GetProperty("content").GetString() == longInput, "automatic model failover preserves the entire original input");
            var price = body.RootElement.GetProperty("provider").GetProperty("max_price");
            check(price.GetProperty("prompt").GetDecimal() == 0 && price.GetProperty("completion").GetDecimal() == 0 && body.RootElement.GetProperty("max_tokens").GetInt32() == 65536, "every fallback retains a zero price ceiling and its provider output maximum");
            return Response(model == primary ? HttpStatusCode.TooManyRequests : HttpStatusCode.OK, model == primary ? "{\"error\":{\"message\":\"upstream model is temporarily busy\"}}" : "{\"choices\":[{\"message\":{\"content\":\"done\"}}]}");
        }))
        {
            var result = FreeCloudCompletion.SendAsync(client, new object[] { new { role = "user", content = longInput } }, primary, "text", false, null, default).GetAwaiter().GetResult();
            check(result.Route.Id == secondary && models.SequenceEqual(new[] { primary, secondary }) && result.Notice.Contains("HTTP 429"), "provider-level saturation automatically tries another verified free model and records the substitution");
        }
        int posts = 0; DateTimeOffset reset = DateTimeOffset.UtcNow.AddMinutes(15);
        using (var client = Client(request => {
            if (request.Method == HttpMethod.Get) return Task.FromResult(Response(HttpStatusCode.OK, catalog));
            posts++; var response = Response(HttpStatusCode.TooManyRequests, "{\"error\":{\"message\":\"Rate limit exceeded: free-models-per-day\"}}");
            response.Headers.RetryAfter = new(reset); return Task.FromResult(response);
        }))
        {
            bool held = false;
            try { FreeCloudCompletion.SendAsync(client, new object[] { new { role = "user", content = "goal" } }, primary, "text", false, null, default).GetAwaiter().GetResult(); }
            catch (ProviderQuotaException ex) { held = ex.RetryAtUtc == reset; }
            check(held && posts == 1, "a shared account quota honors Retry-After and does not hammer other models in the exhausted pool");
        }
        models.Clear();
        using (var client = Client(async request => {
            if (request.Method == HttpMethod.Get) return Response(HttpStatusCode.OK, catalog);
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync()); models.Add(body.RootElement.GetProperty("model").GetString()!);
            return Response(HttpStatusCode.ServiceUnavailable, "{}");
        }))
        {
            bool held = false;
            try { FreeCloudCompletion.SendAsync(client, Array.Empty<object>(), primary, "text", false, null, default).GetAwaiter().GetResult(); }
            catch (ProviderQuotaException) { held = true; }
            check(held && models.SequenceEqual(new[] { primary, secondary }), "unavailable catalogs exhaust each eligible model once, exclude paid/Qwen routes, and hand off to durable automatic recovery");
        }
        posts = 0;
        using (var client = Client(request => { if (request.Method == HttpMethod.Post) posts++; return Task.FromResult(Response(request.Method == HttpMethod.Get ? HttpStatusCode.OK : HttpStatusCode.Unauthorized, request.Method == HttpMethod.Get ? catalog : "{}")); }))
        {
            bool failed = false; try { FreeCloudCompletion.SendAsync(client, [], primary, "text", false, null, default).GetAwaiter().GetResult(); } catch (InvalidOperationException ex) { failed = ex.Message.Contains("credential"); }
            check(failed && posts == 1, "invalid credentials remain actionable rather than cycling through models");
        }
        models.Clear();
        using (var client = Client(async request => {
            if (request.Method == HttpMethod.Get) return Response(HttpStatusCode.OK, catalog);
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync()); string model = body.RootElement.GetProperty("model").GetString()!; models.Add(model);
            if (model == primary) throw new HttpRequestException("synthetic network failure");
            return Response(HttpStatusCode.OK, "{\"choices\":[{\"message\":{\"content\":\"done\"}}]}");
        }))
        {
            var result = FreeCloudCompletion.SendAsync(client, [], primary, "text", false, null, default).GetAwaiter().GetResult();
            check(result.Route.Id == secondary && models.Count == 2 && result.Notice.Contains("transport"), "a transport failure can recover through another verified free model");
        }
        posts=0;int catalogs=0;
        using(var client=Client(request=>{
            if(request.Method==HttpMethod.Post)posts++;
            if(request.RequestUri!.AbsolutePath.EndsWith("/models"))catalogs++;
            return Task.FromResult(Response(HttpStatusCode.OK,"{\"data\":{\"free_model_daily_requests\":{\"used\":50,\"limit\":50,\"remaining\":0}}}"));
        }))
        {
            bool held=false;var nextDay=new DateTimeOffset(DateTime.UtcNow.Date.AddDays(1),TimeSpan.Zero);
            try{FreeCloudCompletion.SendAsync(client,[],primary,"text",false,null,default).GetAwaiter().GetResult();}
            catch(ProviderQuotaException ex){held=ex.RetryAtUtc==nextDay;}
            check(held&&posts==0&&catalogs==0,"a known exhausted daily pool waits for UTC reset without wasting completion requests or cycling models");
        }
        posts=0;
        using(var client=Client(request=>{
            if(request.Method==HttpMethod.Get)return Task.FromResult(Response(HttpStatusCode.OK,catalog));
            posts++;return Task.FromResult(Response(HttpStatusCode.TooManyRequests,"{\"error\":{\"message\":\"free-models-per-day\"}}"));
        }))
        {
            bool held=false;var nextDay=new DateTimeOffset(DateTime.UtcNow.Date.AddDays(1),TimeSpan.Zero);
            try{FreeCloudCompletion.SendAsync(client,[],primary,"text",false,null,default).GetAwaiter().GetResult();}
            catch(ProviderQuotaException ex){held=ex.RetryAtUtc==nextDay;}
            check(held&&posts==1,"a daily quota error without Retry-After is scheduled for the UTC daily reset rather than repeated every few minutes");
        }
        posts = 0; using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        using (var client = Client(request => { posts++; return Task.FromResult(Response(HttpStatusCode.OK, catalog)); }))
        {
            bool stopped = false; try { FreeCloudCompletion.SendAsync(client, [], primary, "text", false, null, cancelled.Token).GetAwaiter().GetResult(); } catch (OperationCanceledException) { stopped = true; }
            check(stopped && posts == 0, "user cancellation never triggers automatic model failover or another request");
        }
    }
    private static HttpClient Client(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) => new(new Handler(send)) { BaseAddress = new("https://test.invalid/api/v1/") };
    private static HttpResponseMessage Response(HttpStatusCode status, string body) => new(status) { Content = new StringContent(body) };
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) { cancellationToken.ThrowIfCancellationRequested(); return send(request); }
    }
}
