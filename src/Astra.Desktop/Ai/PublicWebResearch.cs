using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using AstraOS.Core;

namespace AstraOS.Desktop.Ai;

public sealed record WebEvidence(string Title,string Url,string Text,DateTimeOffset RetrievedAt);
public static class PublicWebResearch
{
    private static readonly TimeSpan MatchLimit=TimeSpan.FromMilliseconds(250);
    private static string Clean(string html)
    {
        html=Regex.Replace(html,@"<(script|style|noscript|svg|form)\b[^>]*>[\s\S]*?</\1>"," ",RegexOptions.IgnoreCase,MatchLimit);
        html=Regex.Replace(html,@"<[^>]+>"," ",RegexOptions.None,MatchLimit);
        return Regex.Replace(WebUtility.HtmlDecode(html),@"\s+"," ",RegexOptions.None,MatchLimit).Trim();
    }
    public static async Task<WebEvidence> ReadAsync(string url,CancellationToken ct)
    {
        Uri uri=PublicWebAddress.Parse(url);
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct);timeout.CancelAfter(TimeSpan.FromSeconds(18));ct=timeout.Token;
        using var handler=new SocketsHttpHandler{UseProxy=false,UseCookies=false,AllowAutoRedirect=false,AutomaticDecompression=DecompressionMethods.GZip|DecompressionMethods.Deflate};
        handler.ConnectCallback=async(context,token)=>
        {
            var addresses=await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host,token);
            if(addresses.Length==0||addresses.Any(a=>!PublicWebAddress.IsPublic(a)))throw new HttpRequestException("Host web non pubblico.");
            Exception? last=null;
            foreach(var address in addresses)
            {
                var socket=new Socket(address.AddressFamily,SocketType.Stream,ProtocolType.Tcp);
                try{await socket.ConnectAsync(new IPEndPoint(address,context.DnsEndPoint.Port),token);return new NetworkStream(socket,true);}
                catch(Exception ex){socket.Dispose();last=ex;}
            }
            throw new HttpRequestException("Connessione web non disponibile.",last);
        };
        using var http=new HttpClient(handler);http.DefaultRequestHeaders.UserAgent.ParseAdd("ASTRA-Horizon/4.1 (+user-requested-public-research)");
        for(int redirects=0;redirects<5;redirects++)
        {
            using var response=await http.GetAsync(uri,HttpCompletionOption.ResponseHeadersRead,ct);
            if((int)response.StatusCode is >=300 and <=399)
            {
                var next=response.Headers.Location??throw new HttpRequestException("Reindirizzamento senza destinazione.");uri=PublicWebAddress.Parse((next.IsAbsoluteUri?next:new Uri(uri,next)).AbsoluteUri);continue;
            }
            response.EnsureSuccessStatusCode();string mime=response.Content.Headers.ContentType?.MediaType??"";
            if(mime is not("text/html" or "text/plain" or "application/xhtml+xml" or "application/json"))throw new InvalidDataException("La pagina non contiene testo web supportato. Scarica documenti e PDF dal browser e allegali.");
            if(response.Content.Headers.ContentLength>2_000_000)throw new InvalidDataException("Pagina web oltre 2 MB.");
            using var stream=await response.Content.ReadAsStreamAsync(ct);using var memory=new MemoryStream();var buffer=new byte[16384];int count;
            while((count=await stream.ReadAsync(buffer,ct))>0){if(memory.Length+count>2_000_000)throw new InvalidDataException("Pagina web oltre 2 MB.");memory.Write(buffer,0,count);}
            string html=Encoding.UTF8.GetString(memory.ToArray());string title=mime.Contains("html")?Clean(Regex.Match(html,@"<title[^>]*>([\s\S]*?)</title>",RegexOptions.IgnoreCase,MatchLimit).Groups[1].Value):uri.Host;
            string text=mime.Contains("html")?Clean(html):html;
            return new(title.Length==0?uri.Host:title[..Math.Min(title.Length,200)],uri.AbsoluteUri,text[..Math.Min(text.Length,16000)],DateTimeOffset.UtcNow);
        }
        throw new HttpRequestException("Troppi reindirizzamenti web.");
    }
    public static async Task<IReadOnlyList<(string Title,string Url)>> SearchAsync(string query,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(query)||query.Length>500)throw new ArgumentException("Query web: da 1 a 500 caratteri.");
        // Search result links need raw HTML, not the article text reader.
        var url="https://html.duckduckgo.com/html/?q="+Uri.EscapeDataString(query);
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct);timeout.CancelAfter(TimeSpan.FromSeconds(18));
        using var client=new HttpClient(new HttpClientHandler{UseProxy=false,UseCookies=false,AllowAutoRedirect=false});
        client.DefaultRequestHeaders.UserAgent.ParseAdd("ASTRA-Horizon/4.1");
        using var response=await client.GetAsync(url,HttpCompletionOption.ResponseHeadersRead,timeout.Token);response.EnsureSuccessStatusCode();
        using var source=await response.Content.ReadAsStreamAsync(timeout.Token);using var memory=new MemoryStream();var buffer=new byte[8192];int count;
        while((count=await source.ReadAsync(buffer,timeout.Token))>0){if(memory.Length+count>1_000_000)throw new InvalidDataException("Risultati web troppo grandi.");memory.Write(buffer,0,count);}
        var html=Encoding.UTF8.GetString(memory.ToArray());var result=new List<(string,string)>();
        foreach(Match match in Regex.Matches(html,"<a\\b(?=[^>]*class=[\"'][^\"']*result__a)[^>]*href=[\"']([^\"']+)[\"'][^>]*>([\\s\\S]*?)</a>",RegexOptions.IgnoreCase,MatchLimit))
        {
            try
            {
                string href=WebUtility.HtmlDecode(match.Groups[1].Value);var uri=new Uri(new Uri(url),href);
                if(uri.Host.EndsWith("duckduckgo.com",StringComparison.OrdinalIgnoreCase))
                {
                    string? encoded=uri.Query.TrimStart('?').Split('&').FirstOrDefault(p=>p.StartsWith("uddg="));
                    if(encoded is null)continue;uri=PublicWebAddress.Parse(Uri.UnescapeDataString(encoded[5..]));
                }
                else uri=PublicWebAddress.Parse(uri.AbsoluteUri);
                string title=Clean(match.Groups[2].Value);if(result.All(r=>r.Item2!=uri.AbsoluteUri))result.Add((title,uri.AbsoluteUri));
                if(result.Count>=6)break;
            }catch(ArgumentException){}catch(UriFormatException){}
        }
        if(result.Count==0)throw new HttpRequestException("Il motore non ha restituito risultati leggibili. Usa Apri ricerca nel browser; non viene aggirata una pagina di verifica.");
        return result;
    }
}
