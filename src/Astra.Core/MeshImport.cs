using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace AstraOS.Core;

public sealed record MeshDocument(string Name,string Format,byte[] Bytes,int Vertices,int Triangles)
{
    public object ToWebData()=>new{name=Name,format=Format,data=Convert.ToBase64String(Bytes),vertices=Vertices,triangles=Triangles};
}
public static class MeshImport
{
    public const int MaxBytes=64_000_000;
    public static MeshDocument Parse(string name,byte[] bytes)
    {
        if(bytes.Length is <16 or >MaxBytes)throw new InvalidDataException(AstraOS.Core.UiText.T("Modello mancante o superiore a 64 MB."));
        string ext=Path.GetExtension(name).ToLowerInvariant();int vertices=0,triangles=0;
        switch(ext)
        {
            case ".glb":
                if(BinaryPrimitives.ReadUInt32LittleEndian(bytes)!=0x46546c67||BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4))!=2||BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(8))!=bytes.Length)throw new InvalidDataException(AstraOS.Core.UiText.T("Contenitore GLB 2.0 non valido."));
                int length=checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(12)));
                if(length<2||length>bytes.Length-20||BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(16))!=0x4E4F534A)throw new InvalidDataException(AstraOS.Core.UiText.T("Blocco JSON GLB non valido."));
                (vertices,triangles)=ValidateGltf(bytes.AsMemory(20,length));break;
            case ".gltf":(vertices,triangles)=ValidateGltf(bytes);break;
            case ".stl":
                if(bytes.Length>=84&&84L+50L*BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(80))==bytes.Length)
                {
                    triangles=checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(80)));vertices=checked(triangles*3);
                    for(int offset=84;offset<bytes.Length;offset+=50)for(int n=0;n<12;n++){float f=BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset+n*4)));if(!float.IsFinite(f)||Math.Abs(f)>1e12)throw new InvalidDataException(AstraOS.Core.UiText.T("Coordinate STL non finite o eccessive."));}
                }
                else foreach(var line in Encoding.UTF8.GetString(bytes).Split('\n')){var p=line.Trim().Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries);if(p.Length>0&&p[0]=="vertex"){if(p.Length!=4||p.Skip(1).Any(x=>!Finite(x)))throw new InvalidDataException(AstraOS.Core.UiText.T("Vertice STL non valido."));vertices++;}}
                if(triangles==0){if(vertices%3!=0)throw new InvalidDataException(AstraOS.Core.UiText.T("Triangoli STL incompleti."));triangles=vertices/3;}break;
            case ".obj":
                foreach(var line in Encoding.UTF8.GetString(bytes).Split('\n'))
                {
                    var p=line.Trim().Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries);if(p.Length==0)continue;
                    if(p[0] is "v" or "vn" or "vt"){int count=p[0]=="vt"?2:3;if(p.Length<count+1||p.Skip(1).Take(count).Any(x=>!Finite(x)))throw new InvalidDataException(AstraOS.Core.UiText.T("Coordinate OBJ non valide."));if(p[0]=="v")vertices++;}
                    if(p[0]=="f"){if(p.Length<4||p.Length>66)throw new InvalidDataException(AstraOS.Core.UiText.T("Faccia OBJ non valida o superiore a 64 vertici."));foreach(var item in p.Skip(1))foreach(var index in item.Split('/').Where(x=>x.Length>0))if(!int.TryParse(index,NumberStyles.Integer,CultureInfo.InvariantCulture,out var n)||n==0||Math.Abs((long)n)>1_000_000)throw new InvalidDataException(AstraOS.Core.UiText.T("Indice OBJ non valido."));triangles+=p.Length-3;}
                    if(p[0] is "call" or "csh")throw new InvalidDataException(AstraOS.Core.UiText.T("Comandi OBJ non ammessi."));
                }break;
            default:throw new InvalidDataException(AstraOS.Core.UiText.T("Formati supportati: GLB, glTF incorporato, STL, OBJ."));
        }
        if(vertices is <3 or >1_000_000||triangles>1_000_000)throw new InvalidDataException(AstraOS.Core.UiText.T("Il modello deve contenere 3–1.000.000 vertici, massimo 1.000.000 triangoli."));
        return new(Path.GetFileName(name),ext[1..],bytes,vertices,triangles);
    }
    private static bool Finite(string text)=>double.TryParse(text,NumberStyles.Float,CultureInfo.InvariantCulture,out var n)&&double.IsFinite(n)&&Math.Abs(n)<=1e12;
    private static (int,int) ValidateGltf(ReadOnlyMemory<byte> bytes)
    {
        using var doc=JsonDocument.Parse(bytes,new JsonDocumentOptions{MaxDepth=64});var root=doc.RootElement;
        if(root.GetProperty("asset").GetProperty("version").GetString()!="2.0")throw new InvalidDataException(AstraOS.Core.UiText.T("È richiesto glTF 2.0."));
        foreach(var key in new[]{"buffers","images"})if(root.TryGetProperty(key,out var objects))foreach(var item in objects.EnumerateArray())
            if(item.TryGetProperty("uri",out var uri))
            {
                string? value=uri.GetString();
                if(value is null||!value.StartsWith("data:",StringComparison.Ordinal)||!value.Contains(";base64,",StringComparison.Ordinal)||value.Length>MaxBytes*1.4)throw new InvalidDataException(AstraOS.Core.UiText.T("Il modello deve incorporare le risorse: esporta un GLB. URL e file esterni non sono caricati."));
            }
        if(root.TryGetProperty("images",out var images)&&images.GetArrayLength()>32)throw new InvalidDataException(AstraOS.Core.UiText.T("Massimo 32 immagini per modello."));
        var accessors=root.GetProperty("accessors");long vertices=0,triangles=0;
        foreach(var mesh in root.GetProperty("meshes").EnumerateArray())foreach(var primitive in mesh.GetProperty("primitives").EnumerateArray())
        {
            int position=primitive.GetProperty("attributes").GetProperty("POSITION").GetInt32();int count=accessors[position].GetProperty("count").GetInt32();if(count<0)throw new InvalidDataException(AstraOS.Core.UiText.T("Conteggio vertici non valido."));vertices+=count;
            int indices=primitive.TryGetProperty("indices",out var index)?accessors[index.GetInt32()].GetProperty("count").GetInt32():count;triangles+=indices/3;
        }
        if(root.TryGetProperty("nodes",out var nodes))
        {
            if(nodes.GetArrayLength()>2048)throw new InvalidDataException(AstraOS.Core.UiText.T("Massimo 2.048 nodi."));var state=new byte[nodes.GetArrayLength()];
            void Visit(int i,int depth){if(i<0||i>=state.Length||depth>64||state[i]==1)throw new InvalidDataException(AstraOS.Core.UiText.T("Gerarchia glTF ciclica o troppo profonda."));if(state[i]==2)return;state[i]=1;if(nodes[i].TryGetProperty("children",out var children))foreach(var child in children.EnumerateArray())Visit(child.GetInt32(),depth+1);state[i]=2;}
            for(int i=0;i<state.Length;i++)Visit(i,0);
        }
        return(checked((int)vertices),checked((int)triangles));
    }
}
