using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;

namespace AstraOS.Desktop;
public sealed partial class MainWindow
{
    private readonly Dictionary<(UIElement,string),CompositionAnimation> _motionAnimations=new();
    private readonly List<Button> _dockButtons=new();
    private int _motionRevision;
    private readonly Windows.UI.ViewManagement.UISettings _motionSettings=new();
    private bool MotionAllowed=>!_windowClosed&&_windowActive&&!_preferences.EcoMode&&!_preferences.ReducedMotion&&_motionSettings.AnimationsEnabled;
    private static IEnumerable<UIElement> VisualDescendants(DependencyObject root)
    {for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);if(child is UIElement e)yield return e;foreach(var item in VisualDescendants(child))yield return item;}}
    private void InitializeMotion()
    {
        HorizonDock.Loaded+=(_,_)=>
        {
            foreach(var button in VisualDescendants(HorizonDock).OfType<Button>())RegisterDockMotion(button);
        };
        Activated+=(_,_)=>{if(!MotionAllowed)ResetMotion();};Closed+=(_,_)=>ResetMotion();
    }
    private void RegisterDockMotion(Button button)
    {
                if(_dockButtons.Contains(button))return;_dockButtons.Add(button);
                button.SizeChanged+=(_,_)=>button.CenterPoint=new((float)button.ActualWidth/2,(float)button.ActualHeight/2,0);
                button.CenterPoint=new((float)button.ActualWidth/2,(float)button.ActualHeight/2,0);
                button.PointerEntered+=(_,_)=>{int index=_dockButtons.IndexOf(button);for(int i=0;i<_dockButtons.Count;i++)SpringScale(_dockButtons[i],i==index?1.19f:Math.Abs(i-index)==1?1.07f:1);};
                button.PointerExited+=(_,_)=>{foreach(var item in _dockButtons)SpringScale(item,1);};
                button.PointerPressed+=(_,_)=>SpringScale(button,.94f);button.PointerReleased+=(_,_)=>SpringScale(button,1.12f);
    }
    private void UnregisterDockMotion(Button button)
    {
        _dockButtons.Remove(button);
        foreach(var key in _motionAnimations.Keys.Where(k=>ReferenceEquals(k.Item1,button)).ToArray())
        {try{button.StopAnimation(_motionAnimations[key]);}catch(Exception){} _motionAnimations.Remove(key);}
    }
    private void StartMotion(UIElement element,string target,CompositionAnimation animation)
    {
        var key=(element,target);if(_motionAnimations.Remove(key,out var old))element.StopAnimation(old);
        animation.Target=target;element.StartAnimation(animation);_motionAnimations[key]=animation;
    }
    private void SpringScale(UIElement element,float value)
    {
        if(!MotionAllowed){element.Scale=Vector3.One;return;}
        try{var c=CompositionTarget.GetCompositorForCurrentThread();var a=c.CreateSpringVector3Animation();a.FinalValue=new(value,value,1);a.DampingRatio=.78f;a.Period=TimeSpan.FromMilliseconds(85);StartMotion(element,"Scale",a);}catch(Exception){element.Scale=Vector3.One;}
    }
    private void ResetMotion()
    {
        ++_motionRevision;foreach(var ((element,target),animation) in _motionAnimations){try{element.StopAnimation(animation);if(target=="Scale")element.Scale=Vector3.One;else if(target=="Translation")element.Translation=Vector3.Zero;else if(target=="Opacity")element.Opacity=1;}catch(Exception){}}_motionAnimations.Clear();
    }
    private void AnimateNavigation(string page)
    {
        ResetMotion();if(!MotionAllowed)return;int revision=_motionRevision;
        DispatcherQueue.TryEnqueue(()=>
        {
            if(revision!=_motionRevision||!MotionAllowed)return;
            var panel=page switch{"home"=>HomePanel,"files"=>FilesPanel,"profiles"=>ProfilesPanel,"theme"=>ThemePanel,"stem" or "core" or "gaming" or "creator"=>WorkspacePanel,"plugins"=>PluginsPanel,"widgets"=>WidgetsPanel,"assistant"=>AssistantPanel,"multitasking"=>MultitaskPanel,"windows-tools"=>WindowsToolsPanel,_=>null};if(panel is null)return;
            try{var c=CompositionTarget.GetCompositorForCurrentThread();var ease=c.CreateCubicBezierEasingFunction(new(.18f,.85f),new(.22f,1));int i=0;foreach(var child in panel.Children.Take(7)){var a=c.CreateVector3KeyFrameAnimation();a.InsertKeyFrame(0,new(0,22,0));a.InsertKeyFrame(1,Vector3.Zero,ease);a.Duration=TimeSpan.FromMilliseconds(430);a.DelayTime=TimeSpan.FromMilliseconds(i++*32);a.DelayBehavior=AnimationDelayBehavior.SetInitialValueBeforeDelay;StartMotion(child,"Translation",a);}}
            catch(Exception ex){StartupDiagnostics.Record("Motion.optional",ex.Message);ResetMotion();}
        });
    }
    private void AnimateDisclosure(UIElement content)
    {
        if(!MotionAllowed)return;
        try{var c=CompositionTarget.GetCompositorForCurrentThread();var fade=c.CreateScalarKeyFrameAnimation();fade.InsertKeyFrame(0,.25f);fade.InsertKeyFrame(1,1);fade.Duration=TimeSpan.FromMilliseconds(180);StartMotion(content,"Opacity",fade);var slide=c.CreateVector3KeyFrameAnimation();slide.InsertKeyFrame(0,new(0,8,0));slide.InsertKeyFrame(1,Vector3.Zero);slide.Duration=TimeSpan.FromMilliseconds(220);StartMotion(content,"Translation",slide);}catch(Exception){content.Opacity=1;content.Translation=Vector3.Zero;}
    }
}
