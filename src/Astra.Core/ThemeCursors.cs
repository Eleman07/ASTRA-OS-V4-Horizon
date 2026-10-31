using System.Globalization;
namespace AstraOS.Core;
public sealed record CursorPixels(int Size,int HotX,int HotY,byte[] Bgra);
public static class ThemeCursors
{
    public static readonly string[] Roles={"Arrow","Help","AppStarting","Wait","Crosshair","IBeam","NWPen","No","SizeNS","SizeWE","SizeNWSE","SizeNESW","SizeAll","UpArrow","Hand"};
    public static CursorPixels Draw(string role,AstraPalette palette,int size=48)
    {
        if(!Roles.Contains(role)||size is not(32 or 48 or 64))throw new ArgumentException(AstraOS.Core.UiText.T("Puntatore non valido."));var p=AstraPalette.Sanitize(palette);var pixels=new byte[size*size*4];
        byte[] Rgb(string hex)=>Enumerable.Range(0,3).Select(i=>byte.Parse(hex.Substring(1+i*2,2),NumberStyles.HexNumber,CultureInfo.InvariantCulture)).ToArray();var fill=Rgb(p.Blue);var outline=Rgb(AstraPalette.ContrastingText(p.Blue));var accent=Rgb(p.Ice);
        bool Poly(double x,double y,(double X,double Y)[] points){bool inside=false;for(int i=0,j=points.Length-1;i<points.Length;j=i++){var a=points[i];var b=points[j];if((a.Y>y)!=(b.Y>y)&&x<(b.X-a.X)*(y-a.Y)/(b.Y-a.Y)+a.X)inside=!inside;}return inside;}
        double Segment(double x,double y,double ax,double ay,double bx,double by){double dx=bx-ax,dy=by-ay,t=Math.Clamp(((x-ax)*dx+(y-ay)*dy)/(dx*dx+dy*dy),0,1);return Math.Sqrt(Math.Pow(x-ax-dx*t,2)+Math.Pow(y-ay-dy*t,2));}
        var arrow=new(double X,double Y)[]{(4,3),(5,35),(13,28),(20,43),(26,40),(19,26),(31,25)};
        bool Shape(double x,double y)
        {
            if(role is "Arrow" or "Help" or "AppStarting" or "NWPen" or "UpArrow")return Poly(x,y,arrow);
            if(role=="Hand")return Poly(x,y,new(double,double)[]{(18,5),(23,5),(24,22),(27,16),(32,16),(34,18),(38,18),(41,22),(40,34),(34,43),(20,43),(9,30),(8,26),(12,24),(18,29)});
            if(role=="IBeam")return Segment(x,y,24,7,24,41)<1.8||Segment(x,y,17,7,31,7)<1.6||Segment(x,y,17,41,31,41)<1.6;
            if(role is "Wait" or "No"){double r=Math.Sqrt((x-24)*(x-24)+(y-24)*(y-24));return Math.Abs(r-13)<2.4||(role=="No"&&Segment(x,y,15,15,33,33)<2);}
            if(role=="Crosshair")return Segment(x,y,24,8,24,40)<1.6||Segment(x,y,8,24,40,24)<1.6;
            double xx=x-24,yy=y-24;if(role=="SizeNS")(xx,yy)=(yy,xx);else if(role=="SizeNWSE")(xx,yy)=((xx+yy)/Math.Sqrt(2),(yy-xx)/Math.Sqrt(2));else if(role=="SizeNESW")(xx,yy)=((xx-yy)/Math.Sqrt(2),(yy+xx)/Math.Sqrt(2));
            bool Horizontal(double a,double b)=>Math.Abs(b)<1.7&&Math.Abs(a)<16||Poly(a,b,new(double,double)[]{(-18,0),(-10,-7),(-10,7)})||Poly(a,b,new(double,double)[]{(18,0),(10,-7),(10,7)});
            return Horizontal(xx,yy)||(role=="SizeAll"&&Horizontal(yy,xx));
        }
        // Four samples per pixel, with a contrasting outline at every pointer size.
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            int alpha=0;double rr=0,gg=0,bb=0;
            foreach(double oy in new[]{.25,.75})foreach(double ox in new[]{.25,.75})
            {
                double px=(x+ox)*48/size,py=(y+oy)*48/size;bool inside=Shape(px,py),edge=false;
                if(!inside)for(int angle=0;angle<8;angle++)if(Shape(px+Math.Cos(angle*Math.PI/4)*1.2,py+Math.Sin(angle*Math.PI/4)*1.2)){edge=true;break;}
                if(!inside&&!edge)continue;var color=edge?outline:role=="Wait"&&px>24?accent:fill;rr+=color[0];gg+=color[1];bb+=color[2];alpha++;
            }
            int i=(y*size+x)*4;if(alpha>0){pixels[i]=(byte)(bb/alpha);pixels[i+1]=(byte)(gg/alpha);pixels[i+2]=(byte)(rr/alpha);pixels[i+3]=(byte)(255*alpha/4);}
        }
        bool pointed=role is "Arrow" or "Help" or "AppStarting" or "NWPen" or "UpArrow";return new(size,pointed?size*4/48:size/2,pointed?size*3/48:role=="Hand"?size*5/48:size/2,pixels);
    }
    public static byte[] Encode(CursorPixels cursor)
    {
        int size=cursor.Size,maskStride=(size+31)/32*4;using var stream=new MemoryStream();using var writer=new BinaryWriter(stream);
        writer.Write((ushort)0);writer.Write((ushort)2);writer.Write((ushort)1);writer.Write((byte)size);writer.Write((byte)size);writer.Write((byte)0);writer.Write((byte)0);writer.Write((ushort)cursor.HotX);writer.Write((ushort)cursor.HotY);writer.Write(40+size*size*4+maskStride*size);writer.Write(22);
        writer.Write(40);writer.Write(size);writer.Write(size*2);writer.Write((ushort)1);writer.Write((ushort)32);writer.Write(0);writer.Write(size*size*4);for(int i=0;i<4;i++)writer.Write(0);
        for(int y=size-1;y>=0;y--)writer.Write(cursor.Bgra,y*size*4,size*4);
        for(int y=size-1;y>=0;y--){var row=new byte[maskStride];for(int x=0;x<size;x++)if(cursor.Bgra[(y*size+x)*4+3]==0)row[x/8]|=(byte)(0x80>>(x%8));writer.Write(row);}
        return stream.ToArray();
    }
}
