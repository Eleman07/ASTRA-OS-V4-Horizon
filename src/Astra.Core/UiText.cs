using System.Globalization;
using System.Text.Json;

namespace AstraOS.Core;
public static class UiText
{
    private static IReadOnlyDictionary<string,string> _catalog=new Dictionary<string,string>();
    private static IReadOnlyDictionary<string,string> _english=new Dictionary<string,string>();
    public static string Language { get; private set; }="it-IT";
    public static string LanguageName=>CultureInfo.GetCultureInfo(Language).EnglishName;
    public static string DirectoryPath { get; private set; }="";
    public static bool HasCatalog { get; private set; }=true;
    public static event Action? Changed;
    public static void Initialize(string directory,string language)
    {
        try{Language=CultureInfo.GetCultureInfo(language).Name;}catch(CultureNotFoundException){Language="en-US";}
        DirectoryPath=directory;Reload();
    }
    public static void Reload()
    {
        _english=Read(Path.Combine(DirectoryPath,"en.json"));string code=CultureInfo.GetCultureInfo(Language).TwoLetterISOLanguageName;
        string cache=Path.Combine(AstraPaths.UserDataRoot,"languages",code+".json");
        string path=File.Exists(cache)?cache:Path.Combine(DirectoryPath,code+".json");_catalog=Read(path);var sourcePath=Path.Combine(DirectoryPath,"sources.json");string[] sources=File.Exists(sourcePath)?JsonSerializer.Deserialize<string[]>(File.ReadAllText(sourcePath))??Array.Empty<string>():Array.Empty<string>();HasCatalog=code=="it"||(sources.Length>0&&sources.All(_catalog.ContainsKey));Changed?.Invoke();
    }
    public static string T(string source)
    {
        if(_catalog.TryGetValue(source,out var value))return value;
        if(CultureInfo.GetCultureInfo(Language).TwoLetterISOLanguageName=="it")return source;
        return _english.TryGetValue(source,out var english)?english:source;
    }
    public static string F(string format,params object?[] values)=>string.Format(CultureInfo.CurrentCulture,T(format),values);
    public static IReadOnlyDictionary<string,string> English=>_english;
    public static object WebCatalog()=>_english.Keys.Concat(_catalog.Keys).Distinct().ToDictionary(k=>k,k=>T(k));
    public static string SourceFor(string displayed)=>_english.ContainsKey(displayed)?displayed:_english.FirstOrDefault(p=>p.Value==displayed).Key??displayed;
    private static IReadOnlyDictionary<string,string> Read(string path)
    {
        try{if(!File.Exists(path)||new FileInfo(path).Length>5_000_000)return new Dictionary<string,string>();return JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(path))??new();}
        catch(Exception ex) when(ex is IOException or JsonException or UnauthorizedAccessException){return new Dictionary<string,string>();}
    }
}
