using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AstraOS.Core;

public sealed record PluginTool(string Operation, string Name, string Category, string Description, string Example);

/// <summary>Bounded, local data transformations. No eval, processes, files or network.</summary>
public static class PluginLibrary
{
    public static IReadOnlyList<PluginTool> Tools { get; } = new PluginTool[]
    {
        new("text.readingtime", "Reading Compass", "Studio", "Stima il tempo di lettura a 220 parole/minuto.", "Una lettura consapevole parte dalle idee principali."),
        new("text.keywords", "Keyword Lens", "Studio", "Trova le parole ricorrenti, senza inviare il testo a un modello.", "energia massa energia sistema massa energia"),
        new("text.clean", "Text Detox", "Testi", "Normalizza spazi, tabulazioni e righe vuote.", "Testo   con    spazi\n\n\nseconda riga"),
        new("text.unique", "Duplicate Radar", "Dati", "Rimuove le righe duplicate conservando l'ordine.", "alfa\nbeta\nalfa"),
        new("text.sort", "Line Sorter", "Dati", "Ordina le righe in modo deterministico.", "gamma\nalfa\nbeta"),
        new("text.slug", "Slug Studio", "Codice", "Crea uno slug da un titolo, con normalizzazione Unicode.", "Il mio progetto scientifico!"),
        new("text.checklist", "Action Checklist", "Organizzazione", "Trasforma una lista in una checklist Markdown.", "Leggere il capitolo\nRisolvere gli esercizi\nVerificare i risultati"),
        new("text.quote", "Quote Builder", "Testi", "Prepara citazioni Markdown multilinea.", "Un'osservazione\nUna conclusione"),
        new("text.wrap", "Readable Lines", "Testi", "Ridistribuisce il testo su righe da circa 72 caratteri.", "Una descrizione lunga del progetto può diventare più leggibile con righe di larghezza uniforme."),
        new("dev.jsonpretty", "JSON Lens", "Codice", "Valida e formatta JSON con profondità limitata.", "{\"project\":\"ASTRA\",\"values\":[1,2,3]}"),
        new("dev.jsoncompact", "JSON Compact", "Codice", "Valida JSON e rimuove gli spazi superflui.", "{ \"x\": 1, \"y\": [2, 3] }"),
        new("dev.base64encode", "Base64 Encoder", "Codice", "Codifica testo UTF-8 in Base64; non è cifratura.", "Ciao ASTRA"),
        new("dev.base64decode", "Base64 Decoder", "Codice", "Decodifica Base64 UTF-8 con validazione degli errori.", "Q2lhbyBBU1RSQQ=="),
        new("dev.sha256", "Fingerprint SHA-256", "Codice", "Calcola l'impronta del testo inserito.", "ASTRA"),
        new("dev.urlencode", "URL Encoder", "Codice", "Codifica una componente URL, senza aprire collegamenti.", "fisica e matematica"),
        new("dev.urldecode", "URL Decoder", "Codice", "Decodifica una componente URL come testo.", "fisica%20e%20matematica"),
        new("dev.uuid", "UUID Forge", "Codice", "Genera un identificatore UUID v4 locale.", ""),
        new("data.stats", "Measurement Notebook", "STEM", "N, media, mediana, deviazione standard e incertezza standard della media.", "9.8 10.1 9.9 10.2 10.0"),
        new("data.histogram", "Distribution Sketch", "STEM", "Costruisce un istogramma testuale dei campioni numerici.", "1 1 2 3 3 3 4 5 8 9"),
        new("data.zscores", "Z-Score Lab", "STEM", "Standardizza i campioni usando la deviazione standard della popolazione.", "10 12 14 16 18"),
        new("data.outliers", "Outlier Detective", "STEM", "Segnala valori oltre 1,5 IQR; un'indicazione statistica, non una prova di errore.", "1 2 3 4 5 6 7 99"),
        new("data.regression", "Trend Fit", "STEM", "Regressione lineare ai minimi quadrati su coppie JSON [x,y].", "[[1,2],[2,4.1],[3,5.9],[4,8]]"),
        new("data.correlation", "Correlation Compass", "STEM", "Correlazione di Pearson su coppie JSON [x,y]; non implica causalità.", "[[1,3],[2,5],[3,7],[4,8]]"),
        new("data.movingaverage", "Signal Smoother", "STEM", "Media mobile con finestra esplicita, senza inventare campioni iniziali.", "{\"window\":3,\"values\":[1,2,4,8,4,2]}"),
        new("data.csvmarkdown", "Table Publisher", "Dati", "Converte CSV con intestazione in una tabella Markdown.", "nome,valore\na,1\nb,2"),
        new("data.csvjson", "CSV Bridge", "Dati", "Converte CSV numerico/testuale in un array JSON di oggetti.", "tempo,misura\n0,3.5\n1,4.2"),
        new("math.vector3", "Vector Workbench", "STEM", "Prodotto scalare, vettoriale e norme di due vettori 3D.", "[ [1,2,3], [4,5,6] ]"),
        new("math.matrix2", "Matrix Microscope", "STEM", "Determinante, traccia e inversa di una matrice 2×2.", "[[1,2],[3,4]]"),
        new("math.quadratic", "Root Explorer", "STEM", "Risolve ax²+bx+c=0, includendo le radici complesse.", "1 -3 2"),
        new("math.units", "Unit Bridge", "STEM", "Conversioni dimensionali SI, temperatura, angoli e velocità.", "{\"value\":72,\"from\":\"km/h\",\"to\":\"m/s\"}"),
        new("math.complex", "Complex Plane Kit", "STEM", "Somma, prodotto e moduli di due numeri complessi [re,im].", "[[1,2],[3,-1]]"),
        new("study.recall", "Active Recall Cards", "Studio", "Trasforma righe domanda | risposta in schede ripassabili.", "Che cosa è una derivata? | Il tasso di variazione locale.\nUnità della forza? | Newton."),
        new("study.feynman", "Feynman Canvas", "Studio", "Prepara una traccia di spiegazione da completare sul tema indicato.", "Entropia"),
        new("study.labreport", "Lab Report Canvas", "Studio", "Crea la struttura di un rapporto scientifico, senza generare risultati.", "Misura del periodo di un pendolo"),
        new("study.spaced", "Spaced Review Planner", "Organizzazione", "Calcola date di ripasso a +1, +3, +7, +14 e +30 giorni.", "{\"start\":\"2026-10-28\",\"topic\":\"Algebra lineare\"}"),
        new("study.pomodoro", "Focus Blocks", "Organizzazione", "Divide minuti di studio in blocchi da 25 minuti e pause da 5.", "90"),
        new("color.contrast", "Contrast Inspector", "Creator", "Calcola il contrasto WCAG di due colori #RRGGBB.", "#0A1931 #F6FAFD"),
        new("color.palette", "Tint Laboratory", "Creator", "Genera cinque tonalità chiare/scure da un colore esadecimale.", "#4A7FA7")
    };

    public static string Transform(string operation, string input) => operation switch
    {
        "text.readingtime" => $"Parole: {Words(input).Length}\nLettura stimata: {Math.Max(1, (int)Math.Ceiling(Words(input).Length / 220.0))} min (220 parole/min).",
        "text.keywords" => string.Join("\n", Regex.Matches(input.ToLowerInvariant(), @"[\p{L}\p{N}]{3,}", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100)).Select(m => m.Value).GroupBy(x => x).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal).Take(20).Select(g => $"{g.Key}: {g.Count()}")),
        "text.clean" => Regex.Replace(string.Join("\n", Lines(input).Select(s => Regex.Replace(s.Trim(), @"[\t ]+", " "))), @"\n{3,}", "\n\n"),
        "text.unique" => string.Join("\n", Lines(input).Distinct(StringComparer.Ordinal)),
        "text.sort" => string.Join("\n", Lines(input).OrderBy(s => s, StringComparer.OrdinalIgnoreCase)),
        "text.slug" => Regex.Replace(Regex.Replace(RemoveAccents(input.ToLowerInvariant()), @"[^a-z0-9]+", "-"), @"^-|-$", ""),
        "text.checklist" => string.Join("\n", Lines(input).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => "- [ ] " + s.Trim())),
        "text.quote" => string.Join("\n", Lines(input).Select(s => "> " + s)),
        "text.wrap" => Wrap(input),
        "dev.jsonpretty" => Json(input, true), "dev.jsoncompact" => Json(input, false),
        "dev.base64encode" => Convert.ToBase64String(Encoding.UTF8.GetBytes(input)),
        "dev.base64decode" => new UTF8Encoding(false, true).GetString(Convert.FromBase64String(input.Trim())),
        "dev.sha256" => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))),
        "dev.urlencode" => Uri.EscapeDataString(input), "dev.urldecode" => Uri.UnescapeDataString(input),
        "dev.uuid" => Guid.NewGuid().ToString("D"),
        "data.stats" => Statistics(Numbers(input)), "data.histogram" => Histogram(Numbers(input)),
        "data.zscores" => ZScores(Numbers(input)), "data.outliers" => Outliers(Numbers(input)),
        "data.regression" => Fit(input, false), "data.correlation" => Fit(input, true),
        "data.movingaverage" => MovingAverage(input),
        "data.csvmarkdown" => Csv(input, false), "data.csvjson" => Csv(input, true),
        "math.vector3" => Vector(input), "math.matrix2" => Matrix(input),
        "math.quadratic" => Quadratic(Numbers(input)), "math.units" => Units(input), "math.complex" => Complex(input),
        "study.recall" => string.Join("\n\n", Lines(input).Where(s => s.Contains('|')).Select((s,i) => { var p=s.Split('|',2); return $"Scheda {i+1}\nD: {p[0].Trim()}\nR: {p[1].Trim()}"; })),
        "study.feynman" => $"# Spiega: {input.Trim()}\n\n1. Spiegazione con parole semplici:\n\n2. Un esempio concreto:\n\n3. Un'analogia e i suoi limiti:\n\n4. Passaggi che non so ancora spiegare:\n\n5. Verifica su una fonte o un esercizio:\n\nTraccia da completare: non è un testo generato da AI.",
        "study.labreport" => $"# {input.Trim()}\n\n## Domanda e ipotesi\n\n## Materiali e metodo\n\n## Variabili, unità e incertezze\n\n## Dati osservati\n\n## Analisi e riproducibilità\n\n## Limiti e possibili errori\n\n## Conclusioni sostenute dai dati\n\n## Riferimenti\n\nNessuna misura o conclusione è stata inventata.",
        "study.spaced" => Spaced(input), "study.pomodoro" => Pomodoro(input),
        "color.contrast" => Contrast(input), "color.palette" => Palette(input),
        _ => throw new InvalidDataException("Operazione non supportata.")
    };

    private static string[] Words(string s) => s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
    private static string[] Lines(string s) => s.Replace("\r\n", "\n").Replace('\r','\n').Split('\n');
    private static string N(double n) => double.IsFinite(n) ? n.ToString("G8", CultureInfo.InvariantCulture) : throw new InvalidDataException("Risultato numerico non finito.");
    private static JsonDocument Document(string s) => JsonDocument.Parse(s, new JsonDocumentOptions { MaxDepth = 12 });
    private static double Number(JsonElement e) => e.ValueKind == JsonValueKind.Number && e.TryGetDouble(out var n) && double.IsFinite(n) && Math.Abs(n) <= 1e100 ? n : throw new InvalidDataException("Sono richiesti numeri finiti con modulo ≤ 1e100.");
    private static double[] Numbers(string s)
    {
        double[] result;
        if (s.TrimStart().StartsWith('[')) { using var d=Document(s); result=d.RootElement.EnumerateArray().Select(Number).ToArray(); }
        else result = Regex.Split(s.Trim(), @"[\s,;]+").Select(t => double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) && double.IsFinite(n) && Math.Abs(n)<=1e100 ? n : throw new InvalidDataException("Usa numeri con punto decimale, separati da spazi o virgole.")).ToArray();
        if (result.Length is < 1 or > 512) throw new InvalidDataException("Da 1 a 512 campioni.");
        return result;
    }
    private static string RemoveAccents(string s) => new(s.Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
    private static string Wrap(string input) { var b=new StringBuilder(); int width=0; foreach(var w in Words(input)) { if(width>0 && width+w.Length+1>72){b.AppendLine();width=0;} if(width>0){b.Append(' ');width++;} b.Append(w);width+=w.Length;} return b.ToString(); }
    private static string Json(string s, bool indent) { using var d=Document(s); return JsonSerializer.Serialize(d.RootElement,new JsonSerializerOptions{WriteIndented=indent}); }
    private static double Quantile(double[] sorted,double p) { double i=(sorted.Length-1)*p; int a=(int)i; return sorted[a]+(sorted[Math.Min(a+1,sorted.Length-1)]-sorted[a])*(i-a); }
    private static string Statistics(double[] x) { double m=x.Average(), ss=x.Sum(v=>(v-m)*(v-m)); var sorted=x.Order().ToArray(); return $"n: {x.Length}\nMedia: {N(m)}\nMediana: {N(Quantile(sorted,.5))}\nMin: {N(sorted[0])}\nMax: {N(sorted[^1])}\nDeviazione standard campionaria: {(x.Length>1?N(Math.Sqrt(ss/(x.Length-1))):"non disponibile (n<2)")}\nIncertezza standard della media: {(x.Length>1?N(Math.Sqrt(ss/(x.Length-1)/x.Length)):"non disponibile")}"; }
    private static string Histogram(double[] x) { double lo=x.Min(), hi=x.Max(); if(lo==hi)return $"{N(lo)}: {x.Length} campioni"; int bins=Math.Clamp((int)Math.Ceiling(Math.Sqrt(x.Length)),3,12); var counts=new int[bins]; foreach(var v in x)counts[Math.Min(bins-1,(int)((v-lo)/(hi-lo)*bins))]++; return string.Join("\n",counts.Select((c,i)=>$"{N(lo+(hi-lo)*i/bins)} → {N(lo+(hi-lo)*(i+1)/bins)}: {new string('▇',(int)Math.Round(30.0*c/Math.Max(1,counts.Max())))} {c}")); }
    private static string ZScores(double[] x) { double m=x.Average(),sd=Math.Sqrt(x.Sum(v=>(v-m)*(v-m))/x.Length); if(sd==0)throw new InvalidDataException("Deviazione standard zero: standardizzazione non definita."); return string.Join("\n",x.Select((v,i)=>$"{i+1}: {N((v-m)/sd)}")); }
    private static string Outliers(double[] x) { var s=x.Order().ToArray(); double q1=Quantile(s,.25),q3=Quantile(s,.75),iqr=q3-q1; var outliers=x.Where(v=>v<q1-1.5*iqr||v>q3+1.5*iqr); return $"Q1={N(q1)} · Q3={N(q3)} · IQR={N(iqr)}\nCandidati oltre 1,5 IQR: "+(outliers.Any()?string.Join(", ",outliers.Select(N)):"nessuno"); }
    private static double[][] Pairs(string input,int width=2) { using var d=Document(input); var a=d.RootElement.EnumerateArray().Select(r=>r.EnumerateArray().Select(Number).ToArray()).ToArray(); if(a.Length is <2 or >200||a.Any(r=>r.Length!=width))throw new InvalidDataException($"Atteso array di righe da {width} numeri."); return a; }
    private static string Fit(string input,bool correlation) { var p=Pairs(input); double mx=p.Average(r=>r[0]),my=p.Average(r=>r[1]),xx=p.Sum(r=>(r[0]-mx)*(r[0]-mx)),yy=p.Sum(r=>(r[1]-my)*(r[1]-my)),xy=p.Sum(r=>(r[0]-mx)*(r[1]-my)); if(xx==0||correlation&&yy==0)throw new InvalidDataException("Varianza nulla: risultato non definito."); return correlation?$"r di Pearson: {N(xy/Math.Sqrt(xx)/Math.Sqrt(yy))}\nn={p.Length}; correlazione non implica causalità.":$"y = {N(xy/xx)} x + {N(my-xy/xx*mx)}\nn={p.Length}\nR²: {(yy>0?N(Math.Clamp(xy/Math.Sqrt(xx)/Math.Sqrt(yy),-1,1)*Math.Clamp(xy/Math.Sqrt(xx)/Math.Sqrt(yy),-1,1)):"non definito (Y costante)")}"; }
    private static string MovingAverage(string input) { using var d=Document(input); int w=d.RootElement.GetProperty("window").GetInt32(); var x=d.RootElement.GetProperty("values").EnumerateArray().Select(Number).ToArray(); if(w<1||w>x.Length||x.Length>512)throw new InvalidDataException("Finestra tra 1 e numero dei campioni (massimo 512)."); return string.Join("\n",Enumerable.Range(w-1,x.Length-w+1).Select(i=>$"{i}: {N(x.Skip(i-w+1).Take(w).Average())}")); }
    private static string Csv(string input,bool json)
    {
        using var parser=new Microsoft.VisualBasic.FileIO.TextFieldParser(new StringReader(input)){TextFieldType=Microsoft.VisualBasic.FileIO.FieldType.Delimited,HasFieldsEnclosedInQuotes=true}; parser.SetDelimiters(","); var headers=parser.ReadFields()??throw new InvalidDataException("CSV vuoto."); if(headers.Length>32||headers.Distinct().Count()!=headers.Length)throw new InvalidDataException("Intestazioni CSV duplicate o troppe colonne."); var rows=new List<string[]>(); while(!parser.EndOfData){var r=parser.ReadFields()!;if(r.Length!=headers.Length||rows.Count>=100)throw new InvalidDataException("Righe incoerenti o oltre 100 righe.");rows.Add(r);} if(json)return JsonSerializer.Serialize(rows.Select(r=>headers.Select((h,i)=>(h,r[i])).ToDictionary(v=>v.h,v=>v.Item2)),new JsonSerializerOptions{WriteIndented=true}); string Cell(string s)=>s.Replace("|","\\|").Replace("\n"," "); return "| "+string.Join(" | ",headers.Select(Cell))+" |\n| "+string.Join(" | ",headers.Select(_=>"---"))+" |\n"+string.Join("\n",rows.Select(r=>"| "+string.Join(" | ",r.Select(Cell))+" |"));
    }
    private static string Vector(string s) {var a=Pairs(s,3);if(a.Length!=2)throw new InvalidDataException("Servono due vettori.");var u=a[0];var v=a[1];return $"u·v = {N(u.Zip(v,(x,y)=>x*y).Sum())}\nu×v = ({N(u[1]*v[2]-u[2]*v[1])}, {N(u[2]*v[0]-u[0]*v[2])}, {N(u[0]*v[1]-u[1]*v[0])})\n|u| = {N(Math.Sqrt(u.Sum(x=>x*x)))}\n|v| = {N(Math.Sqrt(v.Sum(x=>x*x)))}";}
    private static string Matrix(string s) {var a=Pairs(s);if(a.Length!=2)throw new InvalidDataException("Serve una matrice 2×2.");double det=a[0][0]*a[1][1]-a[0][1]*a[1][0];return $"Determinante: {N(det)}\nTraccia: {N(a[0][0]+a[1][1])}\n"+(det==0?"Matrice singolare; inversa non definita.":$"Inversa:\n[{N(a[1][1]/det)}, {N(-a[0][1]/det)}]\n[{N(-a[1][0]/det)}, {N(a[0][0]/det)}]");}
    private static string Quadratic(double[] v){if(v.Length!=3||v[0]==0)throw new InvalidDataException("Inserisci a b c; a deve essere diverso da zero.");double a=v[0],b=v[1],c=v[2],d=b*b-4*a*c;if(d<0)return $"x = {N(-b/(2*a))} ± {N(Math.Sqrt(-d)/(2*Math.Abs(a)))} i"; double q=-.5*(b+Math.CopySign(Math.Sqrt(d),b));return q==0?"x₁ = x₂ = 0":$"x₁ = {N(q/a)}\nx₂ = {N(c/q)}";}
    private static string Complex(string s){var a=Pairs(s);if(a.Length!=2)throw new InvalidDataException("Servono due coppie [re,im].");var u=a[0];var v=a[1];return $"Somma: ({N(u[0]+v[0])}, {N(u[1]+v[1])}i)\nProdotto: ({N(u[0]*v[0]-u[1]*v[1])}, {N(u[0]*v[1]+u[1]*v[0])}i)\nModuli: {N(Math.Sqrt(u[0]*u[0]+u[1]*u[1]))}; {N(Math.Sqrt(v[0]*v[0]+v[1]*v[1]))}";}
    private static string Units(string s)
    {
        var units=new Dictionary<string,(string Kind,double Scale,double Offset)>(StringComparer.Ordinal){["m"]=("length",1,0),["km"]=("length",1000,0),["cm"]=("length",.01,0),["mm"]=("length",.001,0),["s"]=("time",1,0),["min"]=("time",60,0),["h"]=("time",3600,0),["kg"]=("mass",1,0),["g"]=("mass",.001,0),["m/s"]=("speed",1,0),["km/h"]=("speed",1/3.6,0),["K"]=("temperature",1,0),["C"]=("temperature",1,273.15),["F"]=("temperature",5.0/9,273.15-32*5.0/9),["rad"]=("angle",1,0),["deg"]=("angle",Math.PI/180,0),["Pa"]=("pressure",1,0),["kPa"]=("pressure",1000,0),["bar"]=("pressure",100000,0),["J"]=("energy",1,0),["kJ"]=("energy",1000,0)};
        using var d=Document(s);double value=Number(d.RootElement.GetProperty("value"));string from=d.RootElement.GetProperty("from").GetString()??"",to=d.RootElement.GetProperty("to").GetString()??"";if(!units.TryGetValue(from,out var f)||!units.TryGetValue(to,out var t)||f.Kind!=t.Kind)throw new InvalidDataException("Unità non supportate o dimensioni incompatibili. Usa m/km/cm/mm, s/min/h, kg/g, m/s/km/h, K/C/F, rad/deg, Pa/kPa/bar, J/kJ.");return $"{N(value)} {from} = {N((value*f.Scale+f.Offset-t.Offset)/t.Scale)} {to}";
    }
    private static string Spaced(string s){using var d=Document(s);if(!DateOnly.TryParseExact(d.RootElement.GetProperty("start").GetString(),"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var date)||date.Year>2100)throw new InvalidDataException("Data iniziale nel formato yyyy-MM-dd, entro il 2100.");var topic=d.RootElement.GetProperty("topic").GetString();return topic+"\n"+string.Join("\n",new[]{1,3,7,14,30}.Select(n=>$"+{n} giorni: {date.AddDays(n):yyyy-MM-dd}"))+"\nPiano locale: nessun evento di calendario creato.";}
    private static string Pomodoro(string s){var v=Numbers(s);if(v.Length!=1||v[0]<1||v[0]>600||v[0]%1!=0)throw new InvalidDataException("Inserisci minuti interi tra 1 e 600.");int left=(int)v[0],i=1;var b=new StringBuilder();while(left>0){int work=Math.Min(25,left);b.AppendLine($"Blocco {i++}: {work} min studio");left-=work;if(left>0)b.AppendLine("Pausa: 5 min");}return b.ToString().TrimEnd();}
    private static (double R,double G,double B) Rgb(string s){if(!Regex.IsMatch(s,@"\A#[0-9a-fA-F]{6}\z"))throw new InvalidDataException("Usa colori #RRGGBB.");return(Convert.ToInt32(s[1..3],16)/255.0,Convert.ToInt32(s[3..5],16)/255.0,Convert.ToInt32(s[5..7],16)/255.0);}
    private static double Luminance((double R,double G,double B) c){double F(double v)=>v<=.04045?v/12.92:Math.Pow((v+.055)/1.055,2.4);return .2126*F(c.R)+.7152*F(c.G)+.0722*F(c.B);}
    private static string Contrast(string s){var parts=Words(s);if(parts.Length!=2)throw new InvalidDataException("Inserisci due colori #RRGGBB.");double a=Luminance(Rgb(parts[0])),b=Luminance(Rgb(parts[1])),ratio=(Math.Max(a,b)+.05)/(Math.Min(a,b)+.05);return $"Contrasto: {N(ratio)}:1\nWCAG AA testo normale (4.5): {(ratio>=4.5?"superato":"non superato")}\nWCAG AA testo grande (3): {(ratio>=3?"superato":"non superato")}";}
    private static string Palette(string s){var c=Rgb(s.Trim());return string.Join("\n",new[]{-.5,-.25,0,.25,.5}.Select(t=>{double F(double x)=>t<0?x*(1+t):x+(1-x)*t;return $"#{(int)Math.Round(F(c.R)*255):X2}{(int)Math.Round(F(c.G)*255):X2}{(int)Math.Round(F(c.B)*255):X2}";}));}
}
