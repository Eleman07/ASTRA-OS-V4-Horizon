using AstraOS.Core;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
internal static class NewExpansion
{
    public static void Run(string root)
    {
        void Check(bool ok,string name){if(!ok)throw new Exception("FAIL: "+name);Console.WriteLine("PASS: "+name);}
        void Reject(Action action,string name){try{action();}catch(InvalidDataException){Check(true,name);return;}throw new Exception("Accepted invalid input: "+name);}
        var models=Path.Combine(root,"src","Astra.Desktop","Graphics3D","web","models");
        using(var catalog=JsonDocument.Parse(File.ReadAllText(Path.Combine(models,"catalog.json"))))foreach(var entry in catalog.RootElement.EnumerateArray())
        {
            var file=entry.GetProperty("file").GetString()!;var bytes=File.ReadAllBytes(Path.Combine(models,file));
            Check(Convert.ToHexString(SHA256.HashData(bytes)).Equals(entry.GetProperty("sha256").GetString(),StringComparison.OrdinalIgnoreCase),"Online mesh integrity: "+file);
            var mesh=MeshImport.Parse(file,bytes);Check(mesh.Vertices>0&&mesh.Triangles>0,"Native GLB validation: "+file);
        }
        Check(MeshImport.Parse("triangle.obj",Encoding.UTF8.GetBytes("v 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 3\n")).Triangles==1,"Imported OBJ triangle");
        Reject(()=>MeshImport.Parse("bad.obj",Encoding.UTF8.GetBytes("v NaN 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 3")),"Nonfinite mesh rejected");
        Reject(()=>MeshImport.Parse("external.gltf",Encoding.UTF8.GetBytes("""{"asset":{"version":"2.0"},"buffers":[{"uri":"https://example.invalid/m.bin"}]}""")),"External mesh resources rejected");
        var now=new DateTimeOffset(2026,3,29,0,0,0,TimeSpan.Zero);var zone=TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        var daily=new AutomationSpec{Trigger="daily",Hour=2,Minute=30,Action="notify",Argument="Test"};var next=WidgetRules.Next(daily,now,zone);
        Check(next>now&&TimeZoneInfo.ConvertTime(next,zone).Hour==3,"Daily schedule handles DST missing hour");
        var interval=new AutomationSpec{IntervalMinutes=7,Action="notify",Argument="Reminder",Enabled=true,NextDue=now};
        Check(WidgetRules.Next(interval,now,zone)==now.AddMinutes(7),"Interval schedule uses real elapsed time");
        var w=new WidgetSpec{Title="Secret widget",Kind="note",Text="personal",Automation=interval};
        Check(WidgetRules.Due(new[]{w},now).Count==1&&WidgetRules.Due(new[]{w with{Automation=interval with{NextDue=now.AddMinutes(7)}}},now).Count==0,"Advance before dispatch prevents duplicate automation");
        Check(WidgetRules.Import(WidgetRules.Export(new[]{w}))[0]==w,"Widget JSON roundtrip");
        Reject(()=>WidgetRules.Validate(w with{Automation=interval with{Action="script"}}),"Widget actions are declarative");
        Reject(()=>WidgetRules.Import("""{"SchemaVersion":1,"Widgets":[null]}"""),"Null widgets rejected");
        var dir=Path.Combine(Path.GetTempPath(),"astra-expansion-"+Guid.NewGuid().ToString("N"));
        try
        {
            var widgets=new EncryptedWidgetStore(dir);widgets.Save(new[]{w});Check(widgets.Load().Single()==w,"DPAPI widgets roundtrip");
            Check(!Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(dir,"widgets.dpapi"))).Contains("Secret widget"),"Widgets encrypted at rest");
            var session=new WorkspaceSession(Guid.NewGuid().ToString("N"),"Lab","stem","engineering","aerospace","grid",Array.Empty<string>(),new[]{root});
            var store=new WorkspaceSessionStore(dir);store.Save(new[]{session});Check(store.Load().Single().Name=="Lab","Profile session DPAPI roundtrip");Reject(()=>WorkspaceSessionStore.Validate(new[]{session,session}),"Duplicate profile sessions rejected");
        }finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
        Check(PerformancePolicy.IsConstrained(4096,8)&&PerformancePolicy.IsConstrained(16384,4)&&!PerformancePolicy.IsConstrained(16384,8),"Capacity policy for 4 GB / low CPU devices");
        Check(PerformancePolicy.InferenceThreads(4096,8)==2&&PerformancePolicy.InferenceThreads(32768,16)<=6,"Inference thread budget bounded on modest hardware");
        var compatibility=System.Text.Json.JsonSerializer.Deserialize<AstraPreferences>("{\"startFullScreen\":true}")!;Check(compatibility.StartFullScreen&&!compatibility.ThemeCursorEnabled,"Fullscreen preference remains compatible; cursor change stays opt-in");
        foreach(double width in new double[]{240,320,390,640,900,1280,1920,3440})foreach(double scale in new double[]{1,1.25,1.5,2}){var banner=BannerLayout.ForWidth(width,scale);Check(banner.TextWidth+2*banner.Padding<=width&&banner.TitleSize is >=27 and <=54&&(!banner.Signature||width>=820&&scale<=1.3),$"Banner bounds and large-text policy: {width}/{scale}");}
        var gaming=GamingAnalysis.Summarize(new[]{new GamingSample(now,20,null,40),new GamingSample(now.AddSeconds(5),40,90,60)});
        Check(gaming.CpuMean==30&&gaming.CpuPeak==40&&gaming.GpuMean==90&&gaming.RamPeak==60,"Gaming measured summary excludes missing GPU samples");
        Check(GamingAnalysis.Csv(new[]{new GamingSample(now,20.5,null,40)}).Contains("20.5,,40"),"Gaming CSV keeps unavailable values empty");
        var cues=SubtitleTimeline.Parse("1\n00:00:01,000 --> 00:00:03,500\nHello\n\n2\n00:00:04,000 --> 00:00:06,000\nWorld");
        var retimed=SubtitleTimeline.Retime(cues,-500,1.1);Check(retimed[0].Start.TotalMilliseconds==600&&retimed[0].End.TotalMilliseconds==3350,"Subtitle offset and drift correction");
        Check(SubtitleTimeline.Parse(SubtitleTimeline.Export(retimed)).SequenceEqual(retimed),"Subtitle export roundtrip");
        Reject(()=>SubtitleTimeline.Parse("1\n00:00:03,000 --> 00:00:01,000\nInvalid"),"Reversed subtitle interval rejected");
        foreach(int size in new[]{32,48,64})foreach(var role in ThemeCursors.Roles)
        {
            var cursor=ThemeCursors.Draw(role,AstraPalette.Default,size);var bytes=ThemeCursors.Encode(cursor);
            Check(bytes[2]==2&&bytes[4]==1&&BitConverter.ToInt32(bytes,18)==22&&cursor.HotX<size&&cursor.HotY<size&&cursor.Bgra.Where((_,i)=>i%4==3).Any(a=>a>0),$"CUR structure and hotspot: {role}/{size}");
        }
        Check(!ThemeCursors.Encode(ThemeCursors.Draw("Arrow",AstraPalette.Default)).SequenceEqual(ThemeCursors.Encode(ThemeCursors.Draw("Arrow",new AstraPalette{Blue="#FF3300"}))),"Pointer geometry adopts actual palette");
        foreach(var layout in new[]{"two","three","grid"})foreach(int n in Enumerable.Range(1,8))
        {
            var tiles=MultitaskLayouts.Arrange(n,-1920,0,1920,1080,layout);
            Check(tiles.Count==n&&tiles.All(r=>r.X>=-1920&&r.Y>=0&&r.X+r.Width<=0&&r.Y+r.Height<=1080),$"Layout negative monitor bounds: {layout}/{n}");
            Check(tiles.SelectMany((a,i)=>tiles.Skip(i+1).Select(b=>a.X+a.Width<=b.X||b.X+b.Width<=a.X||a.Y+a.Height<=b.Y||b.Y+b.Height<=a.Y)).All(x=>x),$"Windows do not overlap: {layout}/{n}");
        }
    }
}
