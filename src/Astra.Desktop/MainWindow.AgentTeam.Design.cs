using System.Text.Json;
using AstraOS.Core;
using AstraOS.Core.Agents;
using AstraOS.Core.SecondBrain;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace AstraOS.Desktop;

public sealed partial class MainWindow
{
    private ProgressBar? _teamProgress;
    private TextBlock? _teamStepStatus, _teamJobCount, _teamEmpty, _teamVaultStatus;
    private string _teamJobsFingerprint = "";
    private TextBox? _teamObjectiveInput, _teamSourceInput;
    private UIElement? _teamWorkspaceView;
    private readonly List<Button> _teamPresetButtons = new();
    private readonly List<(string Id, CheckBox Box)> _teamSpecialistChoices = new();

    private Border TeamSurface(UIElement content) => new() { Child = content, Style = (Style)AppRoot.Resources["Card"], Padding = new Thickness(20) };
    private TextBlock TeamHeading(string text, double size = 19)
    {
        var label = Label(text, size); label.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold; return label;
    }
    private Border TeamTag(string text) => new() { Child = Label(text, 11), Padding = new Thickness(10, 5, 10, 5), CornerRadius = new CornerRadius(8), Background = Token("AstraNavy") };

    private UIElement BuildAgentTeam()
    {
        var config = _teamStore.Configuration;
        _teamJobsFingerprint = "";
        _teamPresetButtons.Clear(); _teamSpecialistChoices.Clear();
        var panel = new StackPanel { Spacing = 16 };
        _teamStatus = Label("Describe an outcome, choose a workflow, and let ASTRA build a traceable deliverable.", 12);

        var hero = new StackPanel { Spacing = 10 };
        hero.Children.Add(Label("YOUR CONNECTED WORKSPACE", 11));
        hero.Children.Add(TeamHeading("ASTRA Assistant", 30));
        hero.Children.Add(Label("Turn your knowledge into projects, understanding and finished work.", 16));
        var identity = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        identity.Children.Add(TeamTag("Nemotron 3 Ultra · OpenRouter"));
        identity.Children.Add(TeamTag("Obsidian knowledge"));
        identity.Children.Add(TeamTag("Specialist agents + skills"));
        hero.Children.Add(identity);
        hero.Children.Add(Label("Plan → specialists → evidence review → final deliverable. Sources and tool calculations remain visible throughout the work.", 12));
        panel.Children.Add(TeamSurface(hero));

        var goal = new TextBox { Header = "What should ASTRA deliver?", PlaceholderText = "Describe your objective, constraints, available data and what a successful result looks like…", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 170, MaxHeight = 520 };
        var sources = new TextBox { Header = "Source material and detailed requirements", PlaceholderText = "Paste long notes, a project brief, paper passages, code, a syllabus or game telemetry. The complete text is kept for this job.", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 130, MaxHeight = 500 };
        _teamObjectiveInput = goal; _teamSourceInput = sources;
        var researchArea = new TextBox { Header = "Subject / scientific area", Text = config.ResearchArea, PlaceholderText = "Aerospace · orbital mechanics · control · physics · mathematics…" };
        var choices = _teamSpecialistChoices;

        var launchpad = new StackPanel { Spacing = 10 };
        launchpad.Children.Add(TeamHeading("Start from a workflow"));
        launchpad.Children.Add(Label("Each workflow defines concrete outputs. Add your own constraints before starting.", 12));
        var presetGrid = new Grid { ColumnSpacing = 10, RowSpacing = 10 };
        presetGrid.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
        presetGrid.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
        for (int i = 0; i < 4; i++) presetGrid.RowDefinitions.Add(new() { Height = GridLength.Auto });
        var presets = new[]
        {
            new TeamPresetDefinition("01  Technical project", "Requirements, architecture, calculations, code and verification", "Develop a STEM / aerospace technical project. Deliver a requirements matrix, assumptions and units, system architecture, governing equations, concrete implementation artifacts, validation cases with tolerances, risks and a reproducible next-step plan. Distinguish tool-computed results from proposed code.\n\nProject objective:\n", ["aerospace", "engineering"]),
            new TeamPresetDefinition("02  Study tutor", "Flashcards, mind map, exercises and a mock exam", "Tutor me in the selected subject using my syllabus and notes. Assess prerequisites, explain the concepts, produce a linked Mermaid mind map, source-backed flashcards, worked exercises followed by unsolved exercises, and a timed mock exam with a separate marking scheme and answer key.\n\nLearning goal / exam:\n", ["study"]),
            new TeamPresetDefinition("03  Scientific research", "Literature matrix, hypotheses and reproducible experiments", "Research the selected STEM area. Use supplied or retrieved sources only. Deliver a literature comparison matrix, research question, evidence gaps, competing hypotheses, reproducible methodology, uncertainty analysis and an experiment plan. Cite source IDs and mark unverified claims.\n\nResearch question:\n", ["research"]),
            new TeamPresetDefinition("04  Gaming lab", "Strategy, accessibility and measured A/B comparisons", "Create a gaming improvement plan for the specified game, version, platform and objective. Deliver a strategy analysis, practice drills, accessible settings, a reproducible A/B experiment, telemetry table and session-review checklist. Use actual measurements; do not invent FPS, latency or patch details.\n\nGame / objective:\n", ["gaming"]),
            new TeamPresetDefinition("05  Creator studio", "Brief, script, storyboard and publication-ready copy", "Build a creator package for my audience and channel: creative brief, finished script/copy, timed storyboard, several hooks, captions, alt text, source/credit register, delivery checklist and a measurable release experiment. Mark rights and factual claims requiring review.\n\nCreative objective:\n", ["creator"]),
            new TeamPresetDefinition("06  Everyday assistant", "Organize, summarize, compare and plan", "Help complete this everyday task. Retrieve relevant vault knowledge, clarify assumptions, produce the actual requested deliverable, organize a practical action list and identify anything that needs my decision.\n\nTask:\n", ["study"]),
            new TeamPresetDefinition("07  Knowledge synthesis", "Connect ideas and surface contradictions", "Synthesize my selected knowledge into atomic notes, a concept map, a decision brief and useful backlinks. Cite exact source IDs, separate inferred connections from explicit links, and list contradictory statements with a verification plan.\n\nTopic:\n", ["research", "study"]),
            new TeamPresetDefinition("08  Evolution & recovery", "Diagnosis, repair proposal, tests and reusable lessons", "Audit ASTRA's supplied diagnostics or source code. Identify a reproducible failure, propose the smallest concrete patch, explain rollback, specify regression tests and record a proposed reusable validation lesson. Never claim a repair was applied or tested without an actual tool record.\n\nIssue / improvement:\n", ["engineering"])
        };
        for (int i = 0; i < presets.Length; i++)
        {
            var preset = presets[i]; var content = new StackPanel { Spacing = 5 };
            content.Children.Add(TeamHeading(preset.Title, 15)); content.Children.Add(Label(preset.Caption, 12));
            var button = new Button { Content = content, Style = (Style)AppRoot.Resources["V4DarkCardButton"], MinHeight = 100, Padding = new Thickness(15), Tag = i };
            _teamPresetButtons.Add(button);
            button.Click += (_, _) => { goal.Text = preset.Prompt; foreach (var choice in choices) choice.Box.IsChecked = preset.Roles.Contains(choice.Id); goal.Focus(FocusState.Programmatic); goal.SelectionStart = goal.Text.Length; _teamStatus.Text = preset.Title + " selected. Add your objective and source material."; };
            Grid.SetColumn(button, i % 2); Grid.SetRow(button, i / 2); presetGrid.Children.Add(button);
        }
        presetGrid.SizeChanged += (_, e) =>
        {
            int columns = e.NewSize.Width < 620 ? 1 : 2;
            while (presetGrid.RowDefinitions.Count < (presets.Length + columns - 1) / columns) presetGrid.RowDefinitions.Add(new() { Height = GridLength.Auto });
            presetGrid.ColumnDefinitions[1].Width = columns == 1 ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
            for (int i = 0; i < presetGrid.Children.Count; i++) { Grid.SetColumn((FrameworkElement)presetGrid.Children[i], i % columns); Grid.SetRow((FrameworkElement)presetGrid.Children[i], i / columns); }
        };
        launchpad.Children.Add(presetGrid); panel.Children.Add(launchpad);

        var compose = new StackPanel { Spacing = 12 };
        compose.Children.Add(TeamHeading("Your next deliverable", 22));
        compose.Children.Add(goal); compose.Children.Add(researchArea);
        compose.Children.Add(new Expander { Header = "Add long source material / requirements", Content = sources, HorizontalAlignment = HorizontalAlignment.Stretch });

        var media = new StackPanel { Spacing = 8 };
        var mediaStatus = Label("Text, documents, audio and images can accompany this objective. Review extracted text before starting.", 12); media.Children.Add(mediaStatus);
        var record = Action("Record voice objective", async (sender, _) =>
        {
            try
            {
                if (!_teamRecorder.IsRecording) { _teamRecorder.Start(); if (sender is Button b) b.Content = "Stop and transcribe"; mediaStatus.Text = "Recording. Select the same button again to stop and transcribe."; }
                else { string file = await _teamRecorder.StopAsync(); try { var input = await AstraOS.Desktop.Ai.AssistantMedia.TranscribeAsync(file, _windowLifetime.Token); goal.Text += (goal.Text.Length > 0 ? "\n" : "") + input.Text; mediaStatus.Text = "Transcript inserted into the objective. Review it before starting."; } finally { File.Delete(file); if (sender is Button b) b.Content = "Record voice objective"; } }
            }
            catch (Exception ex) { mediaStatus.Text = ex.Message; if (sender is Button b) b.Content = "Record voice objective"; }
        }); media.Children.Add(record);
        media.Children.Add(Action("Attach documents, audio or images…", async (_, _) =>
        {
            try
            {
                var picker = new FileOpenPicker(); picker.FileTypeFilter.Add("*");
                WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(this));
                foreach (var file in await picker.PickMultipleFilesAsync())
                {
                    var input = await AstraOS.Desktop.Ai.AssistantMedia.ReadAsync(file.Path, _windowLifetime.Token);
                    if (input.Pixels is not null) { string id = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(input.Pixels)); _teamStore.Images.Put(id, new(input.Name, input.Pixels)); if (!_teamImages.Contains(id)) _teamImages.Add(id); }
                    if (!string.IsNullOrWhiteSpace(input.Text)) sources.Text += "\n\n## Attachment: " + input.Name + "\n" + input.Text;
                }
                mediaStatus.Text = $"{_teamImages.Count} image(s) attached. Extracted text/transcripts are in source material; OCR text alone does not describe image content.";
            }
            catch (Exception ex) { mediaStatus.Text = ex.Message; }
        }));
        media.Children.Add(Action("Clear draft image attachments", (_, _) => { _teamImages.Clear(); mediaStatus.Text = "Draft image attachments cleared."; }));
        compose.Children.Add(new Expander { Header = "Voice, images and document attachments", Content = media, HorizontalAlignment = HorizontalAlignment.Stretch });

        var project = new StackPanel { Spacing = 8 }; var projectLabel = Label("Project folder: not connected", 12);
        project.Children.Add(Action("Connect a project folder…", async (_, _) => { var picker = new FolderPicker(); picker.FileTypeFilter.Add("*"); WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(this)); var folder = await picker.PickSingleFolderAsync(); if (folder is not null) { _agentProjectRoot = folder.Path; projectLabel.Text = "Project: " + folder.Path; } }));
        project.Children.Add(projectLabel); project.Children.Add(Action("Disconnect project folder", (_, _) => { _agentProjectRoot = null; projectLabel.Text = "Project folder: not connected"; }));
        var simulation = new ComboBox { Header = "Deterministic calculation / simulation", DisplayMemberPath = "Name", HorizontalAlignment = HorizontalAlignment.Stretch };
        simulation.Items.Add(new SimulationChoice("", "No simulation attached")); foreach (var tool in SimulationLab.Tools) simulation.Items.Add(new SimulationChoice(tool.Id, tool.Name)); simulation.SelectedIndex = 0; project.Children.Add(simulation);
        var simParameters = new TextBox { Header = "Simulation parameters · JSON", PlaceholderText = "{\"alt\":400,\"j2\":0}", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap }; project.Children.Add(simParameters);
        var repeat = new NumberBox { Header = "Repeat every… minutes · 0 = one job", Minimum = 0, Value = 0 }; project.Children.Add(repeat);
        compose.Children.Add(new Expander { Header = "Project, calculations and recurring work", Content = project, HorizontalAlignment = HorizontalAlignment.Stretch });
        var web = new CheckBox { Content = "Research public web sources for this job", IsChecked = false }; compose.Children.Add(web);
        var start = Action("Start coordinated work  →", (_, _) =>
        {
            try
            {
                string[] selected = choices.Where(c => c.Box.IsChecked == true).Select(c => c.Id).ToArray();
                var parameters = string.IsNullOrWhiteSpace(simParameters.Text) ? new Dictionary<string, double>() : JsonSerializer.Deserialize<Dictionary<string, double>>(simParameters.Text) ?? throw new InvalidDataException("Invalid parameter object.");
                string simulationId = (simulation.SelectedItem as SimulationChoice)?.Id ?? "";
                if (simulationId.Length > 0) { var definition = SimulationLab.Tools.Single(t => t.Id == simulationId); if (parameters.Keys.Any(k => definition.Parameters.All(p => p.Id != k))) throw new InvalidDataException("Unknown simulation parameter."); }
                if (!double.IsFinite(repeat.Value) || repeat.Value < 0) throw new InvalidDataException("Invalid repeat interval.");
                var c = _teamStore.Configuration; c.ResearchArea = researchArea.Text.Trim(); c.PreferFreeCloud = true; c.FallbackToGpu = false; c.SemanticSearch = false; c.UseGpu = false; c.OutputTokens = 0; _teamStore.Settings.Put("runtime", c);
                string context = BuildAssistantContext() + "\n\nSELECTED SUBJECT / SCIENTIFIC AREA:\n" + c.ResearchArea + "\n\nUSER-SUPPLIED SOURCE MATERIAL:\n" + sources.Text;
                var job = _teamStore.Enqueue(goal.Text, selected.Length == 0 ? null : selected, context, _agentProjectRoot ?? "", web.IsChecked == true, repeat.Value);
                _teamStore.Update(job.Id, j => { j.SimulationId = simulationId; j.SimulationParameters = parameters; j.ImageIds = new(_teamImages); });
                _selectedTeamJob = job.Id; StartTeamWorker(); RefreshTeamJobs(); _teamStatus.Text = "Job queued. ASTRA will plan, consult specialists, review evidence and assemble the final deliverable.";
                _teamProgress?.StartBringIntoView();
            }
            catch (Exception ex) { _teamStatus.Text = ex.Message; }
        }); start.Style = (Style)AppRoot.Resources["PrimaryButton"]; start.FontSize = 16; start.Padding = new Thickness(22, 13, 22, 13); compose.Children.Add(start);
        compose.Children.Add(_teamStatus); panel.Children.Add(TeamSurface(compose));

        var memory = new StackPanel { Spacing = 9 }; memory.Children.Add(TeamHeading("Connected knowledge"));
        _teamVaultStatus = Label(string.IsNullOrWhiteSpace(config.VaultPath) ? "Connect an Obsidian vault to retrieve its original Markdown sources." : "Connected vault · " + config.VaultPath, 12); memory.Children.Add(_teamVaultStatus);
        memory.Children.Add(Action("Connect Obsidian vault…", async (_, _) =>
        {
            try { var picker = new FolderPicker(); picker.FileTypeFilter.Add("*"); WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(this)); var folder = await picker.PickSingleFolderAsync(); if (folder is null) return; var c = _teamStore.Configuration; c.VaultPath = ObsidianKnowledge.ValidateVaultPath(folder.Path); _teamStore.Settings.Put("runtime", c); _teamVaultStatus.Text = "Connected vault · " + c.VaultPath; }
            catch (Exception ex) { _teamVaultStatus.Text = ex.Message; }
        }));
        var export = new CheckBox { Content = "Save completed deliverables into the connected vault", IsChecked = config.ExportToVault }; export.Checked += (_, _) => { var c = _teamStore.Configuration; c.ExportToVault = true; _teamStore.Settings.Put("runtime", c); }; export.Unchecked += (_, _) => { var c = _teamStore.Configuration; c.ExportToVault = false; _teamStore.Settings.Put("runtime", c); }; memory.Children.Add(export);
        memory.Children.Add(Label("Relevant note passages accompany online requests. Reports include source references; your vault stays outside the public application repository.", 12));
        memory.Children.Add(Action("Import a Markdown folder into encrypted shared memory…", async (_, _) => { try { var picker = new FolderPicker(); picker.FileTypeFilter.Add("*"); WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(this)); var folder = await picker.PickSingleFolderAsync(); if (folder is not null) { var result = await Task.Run(() => MarkdownVault.Import(_notes, folder.Path, _windowLifetime.Token)); _teamStatus.Text = $"Imported {result.Imported} notes; skipped {result.Skipped}."; } } catch (Exception ex) { _teamStatus.Text = ex.Message; } }));
        panel.Children.Add(new Expander { Header = "Obsidian · sources and result publishing", Content = memory, HorizontalAlignment = HorizontalAlignment.Stretch });

        var roles = new StackPanel { Spacing = 12 };
        foreach (var agent in AgentCatalog.All.Where(a => a.Id is not "coordinator" and not "reviewer"))
        {
            var choice = new CheckBox { Content = agent.Name + " · " + agent.Mission }; choices.Add((agent.Id, choice)); var detail = new StackPanel { Spacing = 4 }; detail.Children.Add(choice); detail.Children.Add(Label(string.Join("  ·  ", agent.Skills), 12)); detail.Children.Add(Label(agent.Method, 11)); roles.Children.Add(detail);
        }
        roles.Children.Add(Label("Leave specialists unselected for automatic routing. Atlas coordinates; Vera reviews evidence before final synthesis.", 12));
        panel.Children.Add(new Expander { Header = "Specialist team · roles and precise skills", Content = roles, HorizontalAlignment = HorizontalAlignment.Stretch });

        var skills = new StackPanel { Spacing = 8 }; skills.Children.Add(Label("Create, edit and import JSON or SKILL.md in the Skills tab, then assign the reusable method here.", 12));
        var roleChoice = new ComboBox { Header = "Agent", ItemsSource = AgentCatalog.All, DisplayMemberPath = "Name", SelectedIndex = 0 }; skills.Children.Add(roleChoice);
        var skillChoice = new ComboBox { Header = "Installed skill", DisplayMemberPath = "Name", HorizontalAlignment = HorizontalAlignment.Stretch }; skillChoice.DropDownOpened += (_, _) => { skillChoice.ItemsSource = _agentSkills.ToArray(); if (skillChoice.SelectedIndex < 0 && _agentSkills.Count > 0) skillChoice.SelectedIndex = 0; }; skills.Children.Add(skillChoice);
        var assigned = Label("", 12); skills.Children.Add(assigned);
        void ShowSkills() { if (roleChoice.SelectedItem is Specialist a) assigned.Text = "Assigned methods: " + string.Join(", ", _teamStore.Configuration.SkillAssignments.GetValueOrDefault(a.Id, Array.Empty<string>())); }
        roleChoice.SelectionChanged += (_, _) => ShowSkills();
        skills.Children.Add(Action("Assign selected skill", (_, _) => { if (roleChoice.SelectedItem is Specialist a && skillChoice.SelectedItem is AgentSkill s) { var c = _teamStore.Configuration; c.SkillAssignments[a.Id] = c.SkillAssignments.GetValueOrDefault(a.Id, Array.Empty<string>()).Append(s.Id).Distinct().ToArray(); _teamStore.Settings.Put("runtime", c); ShowSkills(); } }));
        skills.Children.Add(Action("Remove selected assignment", (_, _) => { if (roleChoice.SelectedItem is Specialist a && skillChoice.SelectedItem is AgentSkill s) { var c = _teamStore.Configuration; c.SkillAssignments[a.Id] = c.SkillAssignments.GetValueOrDefault(a.Id, Array.Empty<string>()).Where(id => id != s.Id).ToArray(); _teamStore.Settings.Put("runtime", c); ShowSkills(); } }));
        panel.Children.Add(new Expander { Header = "Reusable skills · assign methods to agents", Content = skills, HorizontalAlignment = HorizontalAlignment.Stretch });

        var runtime = new StackPanel { Spacing = 9 }; runtime.Children.Add(Label("Online inference uses OpenRouter. ASTRA requires zero-priced routes and keeps errors visible when a free provider is unavailable.", 12));
        runtime.Children.Add(Label(_openRouterCredentials.HasCredential ? "Windows-encrypted OpenRouter credential found." : "Save your OpenRouter key in ASTRA AI settings before starting.", 12));
        var cloudChoice = new ComboBox { Header = "OpenRouter model", HorizontalAlignment = HorizontalAlignment.Stretch }; cloudChoice.Items.Add(new ComboBoxItem { Content = config.CloudModel, Tag = config.CloudModel }); cloudChoice.SelectedIndex = 0;
        cloudChoice.SelectionChanged += (_, _) => { if (cloudChoice.SelectedItem is ComboBoxItem { Tag: string id }) { var c = _teamStore.Configuration; c.CloudModel = id; c.PreferFreeCloud = true; c.FallbackToGpu = false; _teamStore.Settings.Put("runtime", c); } }; runtime.Children.Add(cloudChoice);
        runtime.Children.Add(Action("Refresh available free models", async (_, _) => { try { var models = await AstraOS.Desktop.Ai.LocalLlmSession.GetFreeModelsAsync(_openRouterCredentials.Load() ?? "", _windowLifetime.Token); cloudChoice.Items.Clear(); foreach (var item in models) cloudChoice.Items.Add(new ComboBoxItem { Content = item.Name, Tag = item.Id }); _teamStatus.Text = "Free model catalog refreshed. Availability and provider quotas apply."; } catch (Exception ex) { _teamStatus.Text = ex.Message; } }));
        runtime.Children.Add(TeamHeading("Long input. Complete output.", 16));
        runtime.Children.Add(Label("ASTRA sends the complete supplied text and requests the selected provider's maximum output capacity. Prompt, evidence, reasoning and answer share the upstream model context. The model catalog and provider determine the actual ceilings.", 12));
        runtime.Children.Add(Action("Use provider maximum for long-form work", (_, _) => { var c = _teamStore.Configuration; c.OutputTokens = 0; c.ContextSize = 1_000_000; c.PreferFreeCloud = true; c.FallbackToGpu = false; _teamStore.Settings.Put("runtime", c); _teamStatus.Text = "Provider maximum selected. ASTRA does not silently clip supplied input or final output."; }));
        var modelRole = new ComboBox { Header = "Optional model per specialist", ItemsSource = AgentCatalog.All, DisplayMemberPath = "Name", SelectedIndex = 0 };
        var roleModel = new ComboBox { Header = "Model override · main model is the default", HorizontalAlignment = HorizontalAlignment.Stretch };
        void ShowRoleModel()
        {
            roleModel.Items.Clear(); roleModel.Items.Add(new ComboBoxItem { Content = "Use the main model", Tag = "" }); roleModel.SelectedIndex = 0;
            if (modelRole.SelectedItem is Specialist agent && _teamStore.Configuration.CloudModelAssignments.TryGetValue(agent.Id, out var assignedModel)) { roleModel.Items.Add(new ComboBoxItem { Content = assignedModel, Tag = assignedModel }); roleModel.SelectedIndex = 1; }
        }
        modelRole.SelectionChanged += (_, _) => ShowRoleModel();
        roleModel.DropDownOpened += async (_, _) =>
        {
            try
            {
                string selected = (roleModel.SelectedItem as ComboBoxItem)?.Tag as string ?? "";
                var models = await AstraOS.Desktop.Ai.LocalLlmSession.GetFreeModelsAsync(_openRouterCredentials.Load() ?? "", _windowLifetime.Token);
                roleModel.Items.Clear(); roleModel.Items.Add(new ComboBoxItem { Content = "Use the main model", Tag = "" }); roleModel.SelectedIndex = 0;
                foreach (var entry in models) { var item = new ComboBoxItem { Content = entry.Name, Tag = entry.Id }; roleModel.Items.Add(item); if (entry.Id == selected) roleModel.SelectedItem = item; }
            }
            catch (Exception ex) { _teamStatus.Text = ex.Message; }
        };
        runtime.Children.Add(modelRole); runtime.Children.Add(roleModel); ShowRoleModel();
        runtime.Children.Add(Action("Save specialist model selection", (_, _) => { if (modelRole.SelectedItem is not Specialist agent || roleModel.SelectedItem is not ComboBoxItem { Tag: string id }) return; var c = _teamStore.Configuration; if (id.Length == 0) c.CloudModelAssignments.Remove(agent.Id); else c.CloudModelAssignments[agent.Id] = id; _teamStore.Settings.Put("runtime", c); _teamStatus.Text = agent.Name + ": " + (id.Length == 0 ? "main model" : id); }));
        runtime.Children.Add(Label("Role overrides are explicit and drawn from the current free catalog. The result ledger records which model handled each specialist.", 12));
        var repair = new ToggleSwitch { Header = "Recover recognized temporary worker failures", IsOn = config.AutoRepair }; repair.Toggled += (_, _) => { var c = _teamStore.Configuration; c.AutoRepair = repair.IsOn; _teamStore.Settings.Put("runtime", c); }; runtime.Children.Add(repair);
        var learn = new ToggleSwitch { Header = "Keep reusable, inspectable validation lessons", IsOn = config.AutoLearn }; learn.Toggled += (_, _) => { var c = _teamStore.Configuration; c.AutoLearn = learn.IsOn; _teamStore.Settings.Put("runtime", c); }; runtime.Children.Add(learn);
        runtime.Children.Add(Action("Inspect learned methods", (_, _) => { _teamReport!.Text = string.Join("\n\n", new AgentLearning(_teamStore.Root).List().Select(l => l.AgentId + " · " + l.Category + "\n" + l.Method + "\nEvidence job: " + l.EvidenceJobId)); }));
        var enabled = new ToggleSwitch { Header = "Background worker enabled", IsOn = config.Enabled }; enabled.Toggled += (_, _) => { var c = _teamStore.Configuration; c.Enabled = enabled.IsOn; _teamStore.Settings.Put("runtime", c); if (c.Enabled) StartTeamWorker(); }; runtime.Children.Add(enabled);
        runtime.Children.Add(Action("Start worker when I sign in to Windows", (_, _) => { using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"); key.SetValue("AstraAgentWorker", "\"" + Path.Combine(AppContext.BaseDirectory, "Astra.AgentSupervisor.exe") + "\""); _teamStatus.Text = "Worker startup enabled for this Windows account."; }));
        runtime.Children.Add(Action("Remove worker login startup", (_, _) => { using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true); key?.DeleteValue("AstraAgentWorker", false); _teamStatus.Text = "Worker login startup removed."; }));
        panel.Children.Add(new Expander { Header = "Cloud model, long responses and autonomous recovery", Content = runtime, HorizontalAlignment = HorizontalAlignment.Stretch });

        var activity = new StackPanel { Spacing = 10 }; activity.Children.Add(TeamHeading("Work in progress", 22));
        _teamJobCount = Label("", 12); activity.Children.Add(_teamJobCount); _teamStepStatus = Label("Select a job to inspect its progress.", 13); activity.Children.Add(_teamStepStatus);
        _teamProgress = new ProgressBar { Minimum = 0, Maximum = 100, Value = 0 }; activity.Children.Add(_teamProgress);
        _teamEmpty = Label("Your completed work will appear here with its source ledger and review status.", 13); activity.Children.Add(_teamEmpty);
        _teamJobs = new ListView { MaxHeight = 300, SelectionMode = ListViewSelectionMode.Single }; _teamJobs.SelectionChanged += (_, _) => { if (_refreshingTeam) return; if (_teamJobs.SelectedItem is ListViewItem { Tag: string id }) { _selectedTeamJob = id; RefreshTeamReport(); } }; activity.Children.Add(_teamJobs);
        var nav = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 }; nav.Children.Add(Action("Recent", (_, _) => { _teamPage = 0; RefreshTeamJobs(); })); nav.Children.Add(Action("Older", (_, _) => { _teamPage++; RefreshTeamJobs(); })); activity.Children.Add(nav);
        activity.Children.Add(Action("Resume selected failed job", (_, _) => { try { if (_selectedTeamJob is null) return; _teamStore.Update(_selectedTeamJob, j => { if (j.Status != "Failed") throw new InvalidOperationException("Select a failed job."); j.Status = "Queued"; j.CancelRequested = false; j.Error = ""; }); StartTeamWorker(); RefreshTeamJobs(); } catch (Exception ex) { _teamStatus.Text = ex.Message; } }));
        activity.Children.Add(Action("Cancel selected job and recurring series", (_, _) => { if (_selectedTeamJob is not null) { _teamStore.Cancel(_selectedTeamJob); RefreshTeamJobs(); } })); panel.Children.Add(TeamSurface(activity));
        var result = new StackPanel { Spacing = 10 }; result.Children.Add(TeamHeading("Deliverable & evidence", 22));
        _teamReport = new TextBox { PlaceholderText = "Select a job to read its plan, specialist work, final deliverable and source ledger.", IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 260, MaxHeight = 800 }; result.Children.Add(_teamReport);
        result.Children.Add(Action("Export report as Markdown…", async (_, _) => { if (_selectedTeamJob is null || _teamStore.Records.Get(_selectedTeamJob) is not { } job) return; var picker = new FileSavePicker { SuggestedFileName = "ASTRA-agent-report-" + job.Id[..8] }; picker.FileTypeChoices.Add("Markdown", new List<string> { ".md" }); WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(this)); var file = await picker.PickSaveFileAsync(); if (file is not null) await File.WriteAllTextAsync(file.Path, TeamReport(job)); }));
        result.Children.Add(Action("Save report to encrypted shared memory", (_, _) => { if (_selectedTeamJob is not null && _teamStore.Records.Get(_selectedTeamJob) is { } job) { _notes.Add(job.Goal[..Math.Min(140, job.Goal.Length)], TeamReport(job), new[] { "agent-result", job.Id }); _teamStatus.Text = "Report saved to shared memory."; } }));
        result.Children.Add(Action("Read final deliverable aloud", (_, _) => { if (_selectedTeamJob is not null && _teamStore.Records.Get(_selectedTeamJob) is { } job) { _teamSpeaker?.Dispose(); _teamSpeaker = new(); _teamSpeaker.SetOutputToDefaultAudioDevice(); _teamSpeaker.SpeakAsync(job.Steps.Last().Output); } }));
        result.Children.Add(Action("Stop voice output", (_, _) => _teamSpeaker?.SpeakAsyncCancelAll()));
        result.Children.Add(Action("Export final deliverable as WAV…", async (_, _) => { if (_selectedTeamJob is null || _teamStore.Records.Get(_selectedTeamJob) is not { } job) return; var picker = new FileSavePicker { SuggestedFileName = "astra-voice-report" }; picker.FileTypeChoices.Add("WAV audio", new List<string> { ".wav" }); WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(this)); var file = await picker.PickSaveFileAsync(); if (file is not null) await File.WriteAllBytesAsync(file.Path, await AstraOS.Desktop.Ai.AssistantMedia.SynthesizeAsync(job.Steps.Last().Output, _windowLifetime.Token)); })); panel.Children.Add(TeamSurface(result));
        _teamTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) }; _teamTimer.Tick += (_, _) => { try { if (_v4LastPage == "brain") RefreshTeamJobs(); } catch (Exception ex) { _teamStatus.Text = ex.Message; } };
        AppRoot.Loaded += (_, _) => { _teamTimer.Start(); if (Environment.GetEnvironmentVariable("ASTRA_QA_MODE") != "1" && _teamStore.Configuration.Enabled) StartTeamWorker(); if (Environment.GetEnvironmentVariable("ASTRA_CLOUD_UI_QA") == "1") _ = RunCloudUiSmokeAsync(); else if (Environment.GetEnvironmentVariable("ASTRA_AGENT_QA") == "1") _ = RunAgentBrainSmokeAsync(); };
        Closed += (_, _) => { _teamTimer.Stop(); _teamRecorder.Dispose(); _teamSpeaker?.Dispose(); }; _teamWorkspaceView = panel; RefreshTeamJobs(); return panel;
    }

    private sealed record TeamPresetDefinition(string Title, string Caption, string Prompt, string[] Roles);

    // This native smoke path never starts a worker or calls a model. It only runs
    // when an explicit isolated profile and the standard QA suppression flag exist.
    private async Task RunCloudUiSmokeAsync()
    {
        string? profile = Environment.GetEnvironmentVariable("ASTRA_DATA_DIRECTORY");
        string ordinary = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ASTRA", "OS");
        if (Environment.GetEnvironmentVariable("ASTRA_QA_MODE") != "1" || string.IsNullOrWhiteSpace(profile) || !Path.IsPathFullyQualified(profile) || Path.GetFullPath(profile).TrimEnd(Path.DirectorySeparatorChar).Equals(Path.GetFullPath(ordinary).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase)) return;

        var checks = new List<object>(); bool passed = true;
        void Check(string name, bool okay) { checks.Add(new { name, passed = okay }); passed &= okay; }
        string objective = _teamObjectiveInput?.Text ?? "", source = _teamSourceInput?.Text ?? "";
        try
        {
            await Task.Delay(500); Navigate("brain");
            var tabs = BrainPanel.Children.OfType<TabView>().Single();
            var team = tabs.TabItems.OfType<TabViewItem>().Single(item => ReferenceEquals(item.Content, _teamWorkspaceView));
            tabs.SelectedItem = team;
            Check("Eight profile workflow buttons are available", _teamPresetButtons.Count == 8 && _teamPresetButtons.All(button => button.IsEnabled && button.Content is StackPanel));
            Check("Eight distinct agent roles have precise skills and methods", AgentCatalog.All.Length == 8 && AgentCatalog.All.Select(agent => agent.Id).Distinct().Count() == 8 && AgentCatalog.All.All(agent => agent.Skills.Length > 0 && !string.IsNullOrWhiteSpace(agent.Method)));
            Check("Six selectable specialists accompany the coordinator and reviewer", _teamSpecialistChoices.Select(choice => choice.Id).OrderBy(id => id).SequenceEqual(AgentCatalog.All.Where(agent => agent.Id is not "coordinator" and not "reviewer").Select(agent => agent.Id).OrderBy(id => id)));
            var runtime = _teamStore.Configuration;
            Check("Cloud inference has no local GPU or embedding fallback", runtime.PreferFreeCloud && !runtime.FallbackToGpu && !runtime.UseGpu && !runtime.SemanticSearch);
            Check("Provider maximum output is selected", runtime.OutputTokens == 0);
            Check("Knowledge graph tab is available without loading its renderer", tabs.TabItems.OfType<TabViewItem>().Any(item => item.Header?.ToString() == UiText.T("Knowledge graph")) && _loadBrainGraph is not null);
            if (_teamObjectiveInput is null || _teamSourceInput is null) throw new InvalidOperationException("Cloud composer controls were not initialized.");
            string longObjective = new string('a', 120_000) + "-OBJECTIVE-END";
            string longSource = new string('b', 160_000) + "-SOURCE-END";
            _teamObjectiveInput.Text = longObjective; _teamSourceInput.Text = longSource;
            Check("Objective preserves more than 100,000 characters and its final marker", _teamObjectiveInput.Text == longObjective && _teamObjectiveInput.MaxLength == 0);
            Check("Source material preserves more than 100,000 characters and its final marker", _teamSourceInput.Text == longSource && _teamSourceInput.MaxLength == 0);
            Check("Deliverable control is read-only and accepts complete multiline output", _teamReport is { IsReadOnly: true, AcceptsReturn: true, MaxLength: 0 });
            Check("Native job progress and result panels are present", _teamProgress is not null && _teamStepStatus is not null && _teamJobs is not null && _teamReport is not null);
        }
        catch (Exception ex) { passed = false; checks.Add(new { name = "Native UI smoke exception", detail = ex.ToString(), passed = false }); }
        finally
        {
            if (_teamObjectiveInput is not null) _teamObjectiveInput.Text = objective;
            if (_teamSourceInput is not null) _teamSourceInput.Text = source;
        }

        if (_teamWorkspaceView is FrameworkElement view) view.StartBringIntoView(new BringIntoViewOptions { AnimationDesired = false, VerticalAlignmentRatio = 0 });
        Directory.CreateDirectory(AstraPaths.UserDataRoot);
        await File.WriteAllTextAsync(Path.Combine(AstraPaths.UserDataRoot, "cloud-ui-smoke.json"), JsonSerializer.Serialize(new { passed, modelTested = false, networkTested = false, utc = DateTimeOffset.UtcNow, checks }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
