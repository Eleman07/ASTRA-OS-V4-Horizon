using AstraOS.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
namespace AstraOS.Desktop;
public sealed partial class MainWindow
{
    private void InitializeResponsiveBanner()
    {
        BannerFrame.SizeChanged+=(_,_)=>UpdateResponsiveBanner();
        _motionSettings.TextScaleFactorChanged+=(_,_)=>DispatcherQueue.TryEnqueue(UpdateResponsiveBanner);
        AppRoot.Loaded+=(_,_)=>UpdateResponsiveBanner();
    }
    private void UpdateResponsiveBanner()
    {
        if(_windowClosed||BannerFrame.ActualWidth<=0)return;double w=BannerFrame.ActualWidth,h=BannerFrame.ActualHeight;var layout=BannerLayout.ForWidth(w,_motionSettings.TextScaleFactor);
        BannerFrame.MinHeight=layout.MinimumHeight;BannerCopy.Margin=new Thickness(layout.Padding);BannerCopy.MaxWidth=layout.TextWidth;BannerCopy.Spacing=w<480?10:14;BannerTitle.FontSize=layout.TitleSize;BannerTitle.TextWrapping=TextWrapping.Wrap;BannerTagline.TextWrapping=TextWrapping.Wrap;BannerDescription.MaxWidth=Math.Min(420,layout.TextWidth);BannerSignature.Visibility=layout.Signature?Visibility.Visible:Visibility.Collapsed;
        BannerArtworkImage.Width=w;BannerArtworkImage.Height=h;BannerCanvas.Clip=new RectangleGeometry{Rect=new Windows.Foundation.Rect(0,0,w,h)};
        int columns=DashboardModules.ActualWidth<530?1:DashboardModules.ActualWidth<850?2:3;if(ModuleCards.ColumnDefinitions.Count==columns)return;ModuleCards.ColumnDefinitions.Clear();ModuleCards.RowDefinitions.Clear();for(int i=0;i<columns;i++)ModuleCards.ColumnDefinitions.Add(new ColumnDefinition());for(int i=0;i<(ModuleCards.Children.Count+columns-1)/columns;i++)ModuleCards.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});for(int i=0;i<ModuleCards.Children.Count;i++){Grid.SetColumn((FrameworkElement)ModuleCards.Children[i],i%columns);Grid.SetRow((FrameworkElement)ModuleCards.Children[i],i/columns);}
    }
}
