namespace AstraOS.Core;
public sealed record BannerLayout(double Padding,double TitleSize,double TextWidth,double MinimumHeight,bool Signature)
{
    public static BannerLayout ForWidth(double width,double textScale=1)
    {
        if(!double.IsFinite(width)||width<=0)width=320;if(!double.IsFinite(textScale)||textScale<1)textScale=1;
        double padding=width<480?18:width<900?28:36;
        return new(padding,Math.Clamp(width*.047,27,54),Math.Max(60,Math.Min(width-padding*2,width<600?width:520)),Math.Clamp(width*.25,220,340),width>=820&&textScale<=1.3);
    }
}
