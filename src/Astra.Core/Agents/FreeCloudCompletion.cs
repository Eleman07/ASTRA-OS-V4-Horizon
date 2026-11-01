using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace AstraOS.Core.Agents;

public sealed record FreeCloudResponse(string Json, FreeModelRoute Route, string Notice);

/// <summary>Try compatible zero-price endpoints automatically, without retrying an exhausted account pool.</summary>
public static class FreeCloudCompletion
{
    public static async Task<FreeCloudResponse> SendAsync(HttpClient client, IEnumerable<object> messages, string preferred,
        string modality, bool json, JsonElement? schema, CancellationToken ct)
    {
        await CheckAccountQuotaAsync(client,ct);
        HttpResponseMessage catalogMessage;
        try { catalogMessage = await client.GetAsync("models", ct); }
        catch (HttpRequestException) { throw new ProviderQuotaException("The free-model catalog is temporarily unreachable. ASTRA will recover automatically; no paid or local fallback was used."); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new ProviderQuotaException("The free-model catalog timed out. ASTRA will recover automatically; no paid or local fallback was used."); }
        using var catalogResponse = catalogMessage;
        if (!catalogResponse.IsSuccessStatusCode) throw Failure(catalogResponse.StatusCode, RetryAt(catalogResponse));
        string catalog = await catalogResponse.Content.ReadAsStringAsync(ct);
        var attempted = new HashSet<string>(StringComparer.Ordinal);
        var notices = new List<string>();
        DateTimeOffset? retryAt = null;
        var prepared = messages.ToList();
        if (json || schema.HasValue) prepared.Insert(0, new { role = "system", content = "Return a valid JSON object only, without Markdown fences." + (schema.HasValue ? " Follow this schema: " + schema.Value.GetRawText() : "") });
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            FreeModelRoute route;
            try { route = FreeModelRouter.Resolve(catalog, preferred, modality, attempted); }
            catch (InvalidOperationException) when (attempted.Count > 0)
            {
                throw new ProviderQuotaException("All currently advertised compatible free endpoints were unavailable for this request. ASTRA will retry automatically. No paid or local fallback was used. " + string.Join(" ", notices), retryAt);
            }
            attempted.Add(route.Id);
            var body = new Dictionary<string, object>
            {
                ["model"] = route.Id, ["stream"] = false, ["temperature"] = 0.2, ["messages"] = prepared,
                ["provider"] = new { allow_fallbacks = true, max_price = new { prompt = 0m, completion = 0m, request = 0m, image = 0m } }
            };
            if (route.MaxOutputTokens > 0) body["max_tokens"] = route.MaxOutputTokens;
            using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
                { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") };
            HttpResponseMessage completionMessage;
            try { completionMessage = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct); }
            catch (HttpRequestException)
            {
                notices.Add(route.Id + " encountered a transport failure; automatically trying another compatible zero-price model.");
                continue;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                notices.Add(route.Id + " timed out; automatically trying another compatible zero-price model.");
                continue;
            }
            using var response = completionMessage;
            string payload = await response.Content.ReadAsStringAsync(ct);
            if (response.IsSuccessStatusCode)
                return new(payload, route, string.Join(" ", notices.Append(route.Reason)));
            var reset = RetryAt(response);
            if (response.StatusCode == HttpStatusCode.TooManyRequests && IsAccountQuota(payload))
                throw new ProviderQuotaException("OpenRouter's shared free-account request quota is exhausted. Changing free models cannot reset this pool; ASTRA will recover automatically when service becomes available. No paid or local fallback was used.", reset??(IsDailyAccountQuota(payload)?NextUtcDay():null));
            if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.RequestTimeout
                or HttpStatusCode.TooManyRequests or HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout)
            {
                notices.Add(route.Id + " unavailable (HTTP " + (int)response.StatusCode + "); automatically trying another compatible zero-price model. Equivalent quality is not assumed.");
                if (reset.HasValue && (!retryAt.HasValue || reset < retryAt)) retryAt = reset;
                continue;
            }
            throw Failure(response.StatusCode, reset);
        }
    }

    private static DateTimeOffset NextUtcDay()=>new(DateTime.UtcNow.Date.AddDays(1),TimeSpan.Zero);
    private static bool IsDailyAccountQuota(string payload)=>payload.Contains("free-models-per-day",StringComparison.OrdinalIgnoreCase)
        ||payload.Contains("free_model_daily_requests",StringComparison.OrdinalIgnoreCase)||payload.Contains("daily free request limit",StringComparison.OrdinalIgnoreCase)
        ||payload.Contains("free tier daily quota",StringComparison.OrdinalIgnoreCase);

    // A quota lookup does not consume a generation request. Do not burn the shared free pool on known failures.
    private static async Task CheckAccountQuotaAsync(HttpClient client,CancellationToken ct)
    {
        try
        {
            using var response=await client.GetAsync("key",ct);
            if(response.StatusCode==HttpStatusCode.Unauthorized)throw Failure(response.StatusCode,RetryAt(response));
            if(!response.IsSuccessStatusCode)return;
            using var document=JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if(document.RootElement.TryGetProperty("data",out var data)&&data.ValueKind==JsonValueKind.Object
                &&data.TryGetProperty("free_model_daily_requests",out var quota)&&quota.ValueKind==JsonValueKind.Object
                &&quota.TryGetProperty("remaining",out var remaining)&&remaining.ValueKind==JsonValueKind.Number&&remaining.TryGetInt64(out long count)&&count<=0)
                throw new ProviderQuotaException("OpenRouter reports zero remaining daily free requests. ASTRA preserved this task and will retry automatically after the UTC quota reset; no generation request, paid endpoint or local fallback was used.",RetryAt(response)??NextUtcDay());
        }
        catch(HttpRequestException){ /* Quota telemetry is optional; the actual request still enforces free-only routing. */ }
        catch(JsonException){ /* A changed telemetry schema must not silently authorize a paid route. */ }
        catch(OperationCanceledException)when(!ct.IsCancellationRequested){ }
    }

    private static bool IsAccountQuota(string payload)
    {
        // Provider-specific 429s remain eligible for another model. Never log raw provider bodies.
        string value = payload.ToLowerInvariant();
        return value.Contains("free-models-per-day") || value.Contains("free-models-per-min")
            || value.Contains("free_model_daily_requests") || value.Contains("daily free request limit")
            || value.Contains("free tier daily quota");
    }

    private static DateTimeOffset? RetryAt(HttpResponseMessage response)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (response.Headers.RetryAfter?.Date is { } date) return date > now ? date : now;
        if (response.Headers.RetryAfter?.Delta is { } delta) return now + delta;
        if (response.Headers.TryGetValues("X-RateLimit-Reset", out var values)
            && long.TryParse(values.FirstOrDefault(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long epoch))
        {
            try { return epoch > 10_000_000_000 ? DateTimeOffset.FromUnixTimeMilliseconds(epoch) : DateTimeOffset.FromUnixTimeSeconds(epoch); }
            catch (ArgumentOutOfRangeException) { }
        }
        return null;
    }

    private static Exception Failure(HttpStatusCode status, DateTimeOffset? retryAt) => status switch
    {
        HttpStatusCode.Unauthorized => new InvalidOperationException("OpenRouter did not accept the API credential. Verify the saved key in ASTRA settings."),
        HttpStatusCode.PaymentRequired => new InvalidOperationException("OpenRouter rejected the account balance. ASTRA did not select a paid endpoint."),
        HttpStatusCode.TooManyRequests => new ProviderQuotaException("The free OpenRouter/provider request quota is temporarily exhausted. ASTRA will retry automatically. No paid or local fallback was used.", retryAt),
        HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout => new ProviderQuotaException("OpenRouter is temporarily unavailable. ASTRA will recover automatically; no paid or local fallback was used.", retryAt),
        _ => new HttpRequestException("OpenRouter request failed. HTTP " + (int)status, null, status)
    };
}
