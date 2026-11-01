using AstraOS.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AstraOS.Desktop;

public sealed partial class MainWindow
{
    private UIElement BuildGamingStrategist()
    {
        var p=new StackPanel{Spacing=9};p.Children.Add(Label("SESSION STRATEGIST · YOUR GAMING PLAYBOOK",20));
        p.Children.Add(Label("Turn a goal into a practice plan, an accessibility setup or a controlled settings experiment. Plans use the session measurements actually available in ASTRA.",12));
        var goal=new TextBox{Header="Game, objective and constraints",AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,MinHeight=75,PlaceholderText="Example: improve consistency in ranked play; 45 minutes, controller, readable subtitles."};p.Children.Add(goal);
        p.Children.Add(Action("Build my training and session playbook",(_,_)=>QueueProfileWork("Create a personalized gaming playbook with preparation, a timed practice plan, specific drills, accessibility preferences and a post-session review. Use the supplied game and objective.\n"+goal.Text,new[]{"gaming"},GamingStatus())));
        p.Children.Add(Action("Design a repeatable A/B settings experiment",(_,_)=>QueueProfileWork("Design a controlled gaming settings A/B experiment. Change one setting, keep game version, scene, resolution and run duration fixed, repeat both configurations and define acceptance criteria from available telemetry. Do not claim FPS, latency or temperature measurements.\n"+goal.Text,new[]{"gaming","engineering"},GamingStatus())));
        p.Children.Add(Action("Save a session checkpoint to shared memory",(_,_)=>{_notes.Add("Gaming session checkpoint",goal.Text+"\n"+GamingStatus(),new[]{"gaming","session-checkpoint"});StatusText.Text="Session checkpoint saved; the assistant can retrieve it in future conversations.";}));
        p.Children.Add(Action("Export an A/B experiment worksheet",async(_,_)=>await SaveLabFileAsync("astra-gaming-experiment",".md","# Controlled gaming experiment\n\nObjective: "+goal.Text+"\n\nGame/version:\nScene:\nResolution:\nFixed run duration:\nSingle changed setting:\n\n| Run | Configuration | Measured result | Notes |\n|---|---|---|---|\n| 1 | A | | |\n| 2 | B | | |\n| 3 | A | | |\n| 4 | B | | |\n\n## Available ASTRA measurements\n"+GamingStatus()+"\n\nRecord only metrics actually measured. Note confounders before deciding.")));
        return p;
    }
    private UIElement BuildCreatorLaunchStudio()
    {
        var p=new StackPanel{Spacing=9};p.Children.Add(Label("CREATOR LAUNCH STUDIO · ONE BRIEF, MANY FORMATS",20));
        var brief=new TextBox{Header="Brief, audience, tone and goal",AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,MinHeight=90,PlaceholderText="Example: explain my aerospace project to beginners; clear tone, one 60-second video and a carousel."};p.Children.Add(brief);
        var channels=new TextBox{Header="Delivery formats",Text="60-second video, carousel, short post, newsletter"};p.Children.Add(channels);
        p.Children.Add(Action("Create a coherent content and reuse pack",(_,_)=>QueueProfileWork("Produce actual content for each requested format from one brief. Include an original hook, complete script/copy, storyboard, captions, alt text, CTA, reusable facts and an editorial schedule. Keep claims consistent across formats. Cite supplied sources; flag claims requiring verification.\nBRIEF: "+brief.Text+"\nFORMATS: "+channels.Text,new[]{"creator","research"},CreatorDeliveryAudit())));
        p.Children.Add(Action("Review accessibility and delivery gaps",(_,_)=>QueueProfileWork("Audit the supplied creator asset metadata and brief. Prioritize missing credits, stated rights, alt text, subtitles, aspect ratios and handoff details. Provide concrete suggested copy and corrections. Do not claim to have watched video or heard audio from metadata.\n"+brief.Text,new[]{"creator"},CreatorDeliveryAudit()+"\n"+CreatorPreflight())));
        p.Children.Add(Action("Export project handoff snapshot",async(_,_)=>await SaveLabFileAsync("astra-creator-handoff",".md","# Creator handoff\n\n## Brief\n"+brief.Text+"\n\n## Formats\n"+channels.Text+"\n\n## Asset evidence\n"+CreatorDeliveryAudit()+"\n\n## Storyboard\n"+CreatorStoryboard()+"\n\n## Delivery checks\n"+CreatorPreflight())));
        p.Children.Add(Action("Save this brief in shared memory",(_,_)=>{if(string.IsNullOrWhiteSpace(brief.Text))throw new InvalidOperationException("Write the brief first.");_notes.Add("Creator project brief",brief.Text+"\nFormats: "+channels.Text,new[]{"creator","brief"});StatusText.Text="Brief saved in the shared Second Brain.";}));return p;
    }
    private void QueueProfileWork(string goal,string[] roles,string evidence)
    {
        var job=_teamStore.Enqueue(goal,roles,BuildAssistantContext()+"\nPROFILE EVIDENCE:\n"+evidence);
        _selectedTeamJob=job.Id;Navigate("brain");
        var tabs=BrainPanel.Children.OfType<TabView>().Single();tabs.SelectedItem=tabs.TabItems.OfType<TabViewItem>().First(t=>t.Header.ToString()=="Agent team");
        RefreshTeamJobs();StartTeamWorker();StatusText.Text="Specialist job queued; its report and sources will remain in your shared memory.";
    }
}
