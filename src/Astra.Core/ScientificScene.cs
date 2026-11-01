using System.Text.Json;

namespace AstraOS.Core;

public sealed record ScenePoint(double X,double Y,double Z);
public sealed record SceneSeries(string Name,string Kind,IReadOnlyList<ScenePoint> Points,int Rows=0,int Columns=0,string? Color=null);
public sealed record ScientificScene(string Title,string XLabel,string YLabel,string ZLabel,IReadOnlyList<SceneSeries> Series)
{
    public const int MaxPoints=50_000;
    public static ScientificScene Parse(string json)
    {
        if(json.Length>8_000_000)throw new InvalidDataException(AstraOS.Core.UiText.T("Scena oltre 8 MB."));
        using var doc=JsonDocument.Parse(json,new JsonDocumentOptions{MaxDepth=12});var root=doc.RootElement;
        CheckObject(root,new[]{"schemaVersion","title","description","axes","series"});
        if(root.GetProperty("schemaVersion").GetInt32()!=1)throw new InvalidDataException(AstraOS.Core.UiText.T("Versione scena non supportata."));
        string title=Text(root,"title","Progetto STEM",120),x="X",y="Y",z="Z";
        if(root.TryGetProperty("axes",out var axes)){CheckObject(axes,new[]{"x","y","z"});x=Text(axes,"x","X",80);y=Text(axes,"y","Y",80);z=Text(axes,"z","Z",80);}
        var list=root.GetProperty("series");if(list.ValueKind!=JsonValueKind.Array||list.GetArrayLength() is <1 or >24)throw new InvalidDataException(AstraOS.Core.UiText.T("Da 1 a 24 serie per scena."));
        var result=new List<SceneSeries>();int total=0;
        foreach(var entry in list.EnumerateArray())
        {
            CheckObject(entry,new[]{"name","kind","points","rows","columns","color"});
            var kind=Text(entry,"kind","points",12);if(kind is not("points" or "line" or "surface"))throw new InvalidDataException(AstraOS.Core.UiText.T("Usa points, line o surface."));
            var raw=entry.GetProperty("points");if(raw.ValueKind!=JsonValueKind.Array||raw.GetArrayLength()==0||raw.GetArrayLength()>MaxPoints-total)throw new InvalidDataException(AstraOS.Core.UiText.T("Massimo 50.000 punti complessivi."));
            var points=new List<ScenePoint>();
            foreach(var point in raw.EnumerateArray())
            {
                if(point.ValueKind!=JsonValueKind.Array||point.GetArrayLength()!=3)throw new InvalidDataException(AstraOS.Core.UiText.T("Ogni punto deve contenere esattamente [x,y,z]."));
                points.Add(new(Number(point[0]),Number(point[1]),Number(point[2])));
            }
            if(kind=="line"&&points.Count<2)throw new InvalidDataException(AstraOS.Core.UiText.T("Una traiettoria richiede almeno due punti."));
            int rows=entry.TryGetProperty("rows",out var r)?r.GetInt32():0,columns=entry.TryGetProperty("columns",out var c)?c.GetInt32():0;
            if(kind=="surface"&&(rows<2||columns<2||(long)rows*columns!=points.Count))throw new InvalidDataException(AstraOS.Core.UiText.T("Griglia surface: rows × columns deve corrispondere ai punti, almeno 2×2."));
            string? color=entry.TryGetProperty("color",out var col)?col.GetString():null;
            if(color is not null&&!System.Text.RegularExpressions.Regex.IsMatch(color,@"\A#[0-9a-fA-F]{6}\z"))throw new InvalidDataException(AstraOS.Core.UiText.T("Colore serie non valido."));
            total+=points.Count;result.Add(new(Text(entry,"name","Serie "+result.Count,80),kind,points,rows,columns,color));
        }
        return new(title,x,y,z,result);
    }
    public static ScientificScene FromDataset(ScientificDataset data,int x,int y,int z,string kind)
    {
        if(x< -1||x>=data.Columns.Count||y<0||y>=data.Columns.Count||z< -1||z>=data.Columns.Count||kind is not("points" or "line"))throw new ArgumentException(AstraOS.Core.UiText.T("Mappatura degli assi non valida."));
        var points=data.Rows.Select((row,i)=>(X:x<0?(double?)i:row[x],Y:row[y],Z:z<0?0:row[z]))
            .Where(p=>p.X.HasValue&&p.Y.HasValue&&p.Z.HasValue).Select(p=>new ScenePoint(p.X!.Value,p.Y!.Value,p.Z!.Value)).ToArray();
        if(points.Length==0||kind=="line"&&points.Length<2)throw new InvalidDataException(AstraOS.Core.UiText.T("Campioni numerici validi insufficienti per gli assi scelti."));
        return new(data.Name,x<0?"Indice riga":data.Columns[x],data.Columns[y],z<0?"Z = 0":data.Columns[z],new[]{new SceneSeries(data.Name,kind,points)});
    }
    public object ToWebData()=>new{title=Title,axes=new{x=XLabel,y=YLabel,z=ZLabel},series=Series.Select(s=>new{name=s.Name,kind=s.Kind,points=s.Points.Select(p=>new[]{p.X,p.Y,p.Z}),rows=s.Rows,columns=s.Columns,color=s.Color})};
    private static double Number(JsonElement e)=>e.ValueKind==JsonValueKind.Number&&e.TryGetDouble(out var n)&&double.IsFinite(n)&&Math.Abs(n)<=1e100?n:throw new InvalidDataException(AstraOS.Core.UiText.T("Coordinate finite con modulo ≤ 1e100 richieste."));
    private static string Text(JsonElement e,string key,string fallback,int limit){var s=e.TryGetProperty(key,out var value)?value.GetString():fallback;return !string.IsNullOrWhiteSpace(s)&&s.Length<=limit&&!s.Any(char.IsControl)?s:throw new InvalidDataException(AstraOS.Core.UiText.T("Etichetta non valida: ")+key);}
    private static void CheckObject(JsonElement e,string[] allowed){if(e.ValueKind!=JsonValueKind.Object)throw new InvalidDataException(AstraOS.Core.UiText.T("Oggetto JSON richiesto."));var names=e.EnumerateObject().Select(p=>p.Name).ToArray();if(names.Distinct(StringComparer.Ordinal).Count()!=names.Length||names.Any(n=>!allowed.Contains(n,StringComparer.Ordinal)))throw new InvalidDataException(AstraOS.Core.UiText.T("Campi sconosciuti o duplicati nella scena."));}
}
