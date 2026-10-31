using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;

namespace AstraOS.Core.Agents;

/// <summary>Verified open weights with resumable, cross-process downloads. Prompts stay local.</summary>
public static class ModelDownloads
{
    private static readonly SemaphoreSlim Gate=new(1,1);
    private static readonly HashSet<string> Verified=new();
    public static async Task<string> EnsureAsync(LocalModelSpec spec,CancellationToken ct)
    {
        await Gate.WaitAsync(ct);
        try
        {
            string path=spec.Path;long Bytes=spec.Bytes;string Sha256=spec.Sha256,Url=spec.Url;Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            if(Verified.Contains(path)&&File.Exists(path)&&new FileInfo(path).Length==Bytes)return path;
            FileStream? lease=null;
            while(lease is null)
            {try{lease=new FileStream(path+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}catch(IOException){await Task.Delay(500,ct);}}
            using(lease)
            {
                async Task<bool> Valid(string file)
                {if(!File.Exists(file)||new FileInfo(file).Length!=Bytes)return false;using var stream=File.OpenRead(file);return Convert.ToHexString(await SHA256.HashDataAsync(stream,ct)).Equals(Sha256,StringComparison.OrdinalIgnoreCase);}
                if(await Valid(path)){Verified.Add(path);return path;}
                string partial=path+".part",state=path+".chunks.json";const int block=16*1024*1024;
                var done=File.Exists(state)?System.Text.Json.JsonSerializer.Deserialize<HashSet<int>>(await File.ReadAllTextAsync(state,ct))??new():new HashSet<int>();
                await using var file=new FileStream(partial,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.Read,4096,FileOptions.Asynchronous|FileOptions.RandomAccess);
                if(file.Length!=Bytes){file.SetLength(Bytes);done.Clear();}
                using var http=new HttpClient(new HttpClientHandler{UseCookies=false}){Timeout=TimeSpan.FromMinutes(2)};
                var sync=new object();
                var pending=Enumerable.Range(0,(int)((Bytes+block-1)/block)).Where(i=>!done.Contains(i)).ToArray();
                await Parallel.ForEachAsync(pending,new ParallelOptions{MaxDegreeOfParallelism=6,CancellationToken=ct},async(index,token)=>
                {
                    long start=(long)index*block,end=Math.Min(Bytes-1,start+block-1);
                    using var request=new HttpRequestMessage(HttpMethod.Get,Url+"?download=true&chunk="+index+"&astra="+DateTimeOffset.UtcNow.ToUnixTimeSeconds());request.Headers.Range=new RangeHeaderValue(start,end);
                    using var response=await http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,token);response.EnsureSuccessStatusCode();
                    if(response.StatusCode!=HttpStatusCode.PartialContent||response.Content.Headers.ContentRange?.From!=start||response.Content.Headers.ContentRange?.To!=end)throw new InvalidDataException("Unexpected model download range.");
                    byte[] bytes=await response.Content.ReadAsByteArrayAsync(token);if(bytes.LongLength!=end-start+1)throw new InvalidDataException("Incomplete model chunk.");
                    await RandomAccess.WriteAsync(file.SafeFileHandle,bytes,start,token);
                    lock(sync){done.Add(index);File.WriteAllText(state,System.Text.Json.JsonSerializer.Serialize(done));File.WriteAllText(path+".progress.json",System.Text.Json.JsonSerializer.Serialize(new{received=done.Sum(i=>Math.Min(block,Bytes-(long)i*block)),total=Bytes}));}
                });
                await file.FlushAsync(ct);file.Close();
                if(!await Valid(partial)){await File.WriteAllTextAsync(state,"[]",ct);throw new InvalidDataException("Local model checksum failed. A clean re-download has been prepared; no invalid model was activated.");}
                File.Move(partial,path,true);Verified.Add(path);return path;
            }
        }
        finally{Gate.Release();}
    }
}
