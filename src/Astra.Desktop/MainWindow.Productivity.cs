using System.Diagnostics;
using System.Globalization;
using AstraOS.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Polyline = Microsoft.UI.Xaml.Shapes.Polyline;
using Windows.Foundation;
using Windows.Storage.Pickers;

namespace AstraOS.Desktop;

public sealed partial class MainWindow
{
    private readonly CancellationTokenSource _windowLifetime = new();
    private readonly SystemMonitor _systemMonitor = new();
    private readonly DispatcherTimer _monitorTimer = new() { Interval = TimeSpan.FromSeconds(5) };
    private readonly Queue<LiveSystemSample> _performanceHistory = new();
    private IReadOnlyList<InstalledApplication> _installedApps = Array.Empty<InstalledApplication>();
    private bool _appsLoaded, _appsLoading, _sampling, _windowClosed;
    private bool _windowActive = true;
    private string _workspace = "core";
    private TextBlock? _workspaceMetrics;
    private TextBlock? _historyText;
    private Polyline? _historyLine;
    private TextBox? _appSearch;
    private ComboBox? _appCategory;
    private ListView? _appList;
    private TextBlock? _appStatus;

    private SolidColorBrush Token(string key) => (SolidColorBrush)AppRoot.Resources[key];
    private TextBlock Label(string text, double size = 14) => new()
    { Text = UiText.T(text), FontSize = size, TextWrapping = TextWrapping.Wrap, Foreground = Token("AstraDeepText") };
    private Border Card(UIElement content)
    { SimplifyTechnicalPanels(content);return new(){Child=content,Style=(Style)AppRoot.Resources["Card"]}; }
    private Button Action(string text, RoutedEventHandler handler)
    {
        var button = new Button { Content = UiText.T(text), CornerRadius = new(8), HorizontalAlignment = HorizontalAlignment.Left };
        button.Click += handler; return button;
    }

    private void InitializeProductivity()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASTRA_DATA_DIRECTORY"))) Title += AstraOS.Core.UiText.T(" [QA]");
        _workspace = _preferences.ActiveWorkspace;
        _monitorTimer.Tick += async (_, _) => await RefreshLiveMetricsAsync();
        AppRoot.Loaded += async (_, _) =>
        {
            UpdateResponsiveLayout(); await RefreshLiveMetricsAsync(); _monitorTimer.Start();
            if (Environment.GetEnvironmentVariable("ASTRA_RUN_SMOKE") == "1" &&
                !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASTRA_DATA_DIRECTORY")))
                await RunDesktopSmokeAsync();
        };
        AppRoot.SizeChanged += (_, _) => UpdateResponsiveLayout();
        Activated += (_, e) =>
        {
            _windowActive = e.WindowActivationState != WindowActivationState.Deactivated;
            if (_windowActive||_gamingRecording) _monitorTimer.Start(); else _monitorTimer.Stop();
            SendRenderActivity();
        };
        Closed += (_, _) =>
        {
            _windowClosed = true; _monitorTimer.Stop(); _windowLifetime.Cancel();
            _windowLifetime.Dispose();
            _ = Task.Run(_systemMonitor.Dispose);
        };
        BuildInstalledAppsView();
        InitializeFiles();
        var deployment = new StackPanel { Spacing = 10 };
        var tools = Label(AstraOS.Core.UiText.T("Verifica su richiesta di ADK, OSCDIMG e DISM."), 12);
        deployment.Children.Add(Label(AstraOS.Core.UiText.T("DEPLOYMENT LAB / STRUMENTI LOCALI"), 16));
        deployment.Children.Add(Action(AstraOS.Core.UiText.T("Verifica strumenti"), async (_, _) => tools.Text = await Task.Run(DeploymentInspector.Read)));
        deployment.Children.Add(tools); IsoPanel.Children.Insert(0, Card(deployment));
        var history = new StackPanel { Spacing = 8 };
        history.Children.Add(Label(AstraOS.Core.UiText.T("CPU DI SISTEMA · ULTIMI 60 CAMPIONI"), 15));
        _historyLine = new Polyline { Stroke = Token("AstraIce"), StrokeThickness = 2, Height = 100, Stretch = Stretch.Fill };
        _historyText = Label(AstraOS.Core.UiText.T("In attesa dei campioni reali. Il monitoraggio si sospende quando la finestra è inattiva."), 12);
        history.Children.Add(_historyLine); history.Children.Add(_historyText);
        DiagnosticPanel.Children.Insert(0, Card(history));
    }

    private void UpdateResponsiveLayout()
    {
        var width = AppRoot.ActualWidth;
        if (width <= 0) return;
        SidebarColumn.Width = new(_preferences.FocusMode||width<780 ? 0 : width < 1100 ? 176 : 218);
        SidebarShell.Visibility=SidebarColumn.Width.Value>0?Visibility.Visible:Visibility.Collapsed;
        L10n0021.Visibility=width<900?Visibility.Collapsed:Visibility.Visible;
        FullScreenButton.Content=width<900?new FontIcon{FontFamily=new FontFamily("Segoe Fluent Icons"),Glyph="\uE740",FontSize=17}:AppWindow.Presenter.Kind==Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen?UiText.T("Torna alla finestra"):UiText.T("Schermo intero");
        MainSurface.Padding=new Thickness(width<780?12:24,14,width<780?12:22,12);
        FocusModeButton.Visibility=width<780?Visibility.Collapsed:Visibility.Visible;
        var narrow = width < 1120;
        WidgetColumn.Width = narrow ? new(0) : new(width < 1420 ? 285 : 326);
        Grid.SetColumn(DashboardWidgets, narrow ? 0 : 1);
        Grid.SetRow(DashboardWidgets, narrow ? 1 : 0);
        V4Clock.Visibility = width < 1000 ? Visibility.Collapsed : Visibility.Visible;
        HorizonDock.MaxWidth = Math.Max(240, width - SidebarColumn.Width.Value - 30);
        UpdateResponsiveBanner();
    }

    private async Task RefreshLiveMetricsAsync(bool force = false)
    {
        while (_sampling && force && !_windowClosed) await Task.Delay(25);
        if (_sampling || _windowClosed || (!force && !_gamingRecording && (!_windowActive ||
            _v4LastPage is not ("home" or "diagnostics" or "core" or "stem" or "gaming" or "creator" or "widgets")))) return;
        _sampling = true;
        try
        {
            var sample = await Task.Run(_systemMonitor.Capture, _windowLifetime.Token);
            if (_windowClosed) return;
            RecordGamingSample(sample);
            _performanceHistory.Enqueue(sample);
            while (_performanceHistory.Count > 60) _performanceHistory.Dequeue();
            V4CpuText.Text = sample.CpuPercent.HasValue ? AstraOS.Core.UiText.F("{0:F1}%", sample.CpuPercent) : AstraOS.Core.UiText.T("Campionamento…");
            V4CpuBar.Value = sample.CpuPercent ?? 0;
            V4RamText.Text = sample.RamPercent.HasValue ? AstraOS.Core.UiText.F("{0:F0}%", sample.RamPercent) : AstraOS.Core.UiText.T("Non disponibile");
            V4RamBar.Value = sample.RamPercent ?? 0;
            V4GpuText.Text = sample.GpuPercent.HasValue ? AstraOS.Core.UiText.F("{0:F1}% · motore più occupato", sample.GpuPercent) : AstraOS.Core.UiText.T("Non disponibile");
            V4DiskText.Text = sample.DiskTotal > 0 ? AstraOS.Core.UiText.F("{0:F0} / {1:F0} GB", (sample.DiskTotal - sample.DiskAvailable) / 1073741824.0, sample.DiskTotal / 1073741824.0) : AstraOS.Core.UiText.T("Non disponibile");
            string details = sample.TotalMemory > 0 ? $"RAM libera {sample.AvailableMemory / 1073741824.0:F1} / {sample.TotalMemory / 1073741824.0:F1} GB" : "RAM: Non disponibile";
            details += $"\n{sample.ProcessCount} processi · {sample.Network}\nBatteria: {sample.Battery}";
            LiveSystemDetails.Text = sample.TotalMemory > 0 ? AstraOS.Core.UiText.F("RAM libera {0:F1} / {1:F1} GB\n{2} processi · batteria: {3}", sample.AvailableMemory / 1073741824.0, sample.TotalMemory / 1073741824.0, sample.ProcessCount, sample.Battery.Split('·')[0].Trim()) : AstraOS.Core.UiText.T("RAM: Non disponibile");
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetHelpText(V4GpuText, "Stima PDH del motore GPU fisico più occupato; non somma motori differenti.");
            _monitorTimer.Interval = TimeSpan.FromSeconds(_preferences.EcoMode ? 15 : 5);
            LiveSampleStatus.Text = AstraOS.Core.UiText.F("{0:HH:mm:ss} · refresh {1:F0} s · locale", sample.Time, _monitorTimer.Interval.TotalSeconds);
            if (_workspaceMetrics is not null) _workspaceMetrics.Text = AstraOS.Core.UiText.F("CPU: {0} · RAM: {1}\n{2}", V4CpuText.Text, V4RamText.Text, details);
            if (_historyText is not null) _historyText.Text = AstraOS.Core.UiText.F("{0}\n{1}\nCampioni: {2} · nessun dato inviato online", sample.Os, details, _performanceHistory.Count);
            if (_historyLine is not null)
            {
                var points = new PointCollection(); int i = 0;
                foreach (var s in _performanceHistory.Where(s => s.CpuPercent.HasValue)) points.Add(new Point(i++ * 8, 100 - s.CpuPercent!.Value));
                _historyLine.Points = points;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            if (!_windowClosed) LiveSampleStatus.Text = AstraOS.Core.UiText.T("Metriche non disponibili: ") + ex.Message;
            StartupDiagnostics.Record("SystemMonitor.failed", ex.ToString());
        }
        finally { _sampling = false; }
    }

    private void WorkspaceNav_Click(object sender, RoutedEventArgs e)
    { if (sender is FrameworkElement { Tag: string route }) Navigate(route); }

    private void OnProductivityNavigate(string page)
    {
        FilesPanel.Visibility=page=="files"?Visibility.Visible:Visibility.Collapsed;
        FilesNav.Background=page=="files"?Token("AstraBlue"):new SolidColorBrush(Windows.UI.Color.FromArgb(0,0,0,0));
        if(page=="files"){PageTitle.Text=AstraOS.Core.UiText.T("File Explorer");PageSubtitle.Text=AstraOS.Core.UiText.T("File e cartelle reali · tema ASTRA");_ = LoadFilesAsync(_filesDirectory);}
        bool workspace = CuratedCatalog.ProfileIds.Contains(page);
        WorkspacePanel.Visibility = workspace ? Visibility.Visible : Visibility.Collapsed;
        foreach (var (button, id) in new[] { (CoreWorkspaceNav,"core"), (StemWorkspaceNav,"stem"),
            (GamingWorkspaceNav,"gaming"), (CreatorWorkspaceNav,"creator") })
            button.Background = page == id ? Token("AstraBlue") : new SolidColorBrush(Windows.UI.Color.FromArgb(0,0,0,0));
        if (workspace)
        {
            _workspace = page;
            if (_preferences.ActiveWorkspace != page)
            {
                _preferences.ActiveWorkspace = page;
                try { _preferenceStore.Save(_preferences); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { StatusText.Text = AstraOS.Core.UiText.T("Workspace non salvato: ") + ex.Message; }
            }
            RenderWorkspace();
        }
        if (page == "catalog") _ = EnsureAppsAsync();
        if (page == "home") _ = RefreshHomeFilesAsync();
        if (page == "home" || workspace || page == "diagnostics") _ = RefreshLiveMetricsAsync();
    }

    private void RenderWorkspace()
    {
        StopStudio();
        WorkspacePanel.Children.Clear();
        PageTitle.Text = AstraOS.Core.UiText.T("ASTRA ") + (_workspace == "stem" ? AstraOS.Core.UiText.T("STEM") : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(_workspace));
        PageSubtitle.Text = _workspace switch { "stem" => AstraOS.Core.UiText.T("Studio STEM · simulazioni del tuo percorso · Python 3D · Data Lab"), "gaming" => AstraOS.Core.UiText.T("Libreria locale · sessione · profili reversibili"), "creator" => AstraOS.Core.UiText.T("Progetti · asset · applicazioni creative"), _ => AstraOS.Core.UiText.T("App recenti · progetti · strumenti Windows") };
        var actions = new StackPanel { Spacing = 10 };
        actions.Children.Add(Label(_workspace=="stem"?StudyCatalog.Name(_preferences.StudyDomain,_preferences.EngineeringTrack):AstraOS.Core.UiText.T("IL TUO WORKSPACE"), 19));
        actions.Children.Add(Label(PageSubtitle.Text, 13));
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        if(_workspace=="stem"&&_preferences.StudyDomain=="engineering"&&_preferences.EngineeringTrack=="aerospace")row.Children.Add(Action(AstraOS.Core.UiText.T("Ariane 64 (ESA)"),async(_,_)=>await ExploreArianeAsync()));
        if(_workspace=="stem"){row.Children.Add(Action(AstraOS.Core.UiText.T("Simulazioni del percorso ↓"),(_,_)=>_simulationCard?.StartBringIntoView(new BringIntoViewOptions{AnimationDesired=MotionAllowed})));row.Children.Add(Action(AstraOS.Core.UiText.T("Python 3D ↓"),(_,_)=>_studioHost?.StartBringIntoView(new BringIntoViewOptions{AnimationDesired=MotionAllowed})));}
        else row.Children.Add(Action(AstraOS.Core.UiText.T("Apri App Hub"), (_, _) => Navigate("catalog")));
        row.Children.Add(Action(AstraOS.Core.UiText.T("Esplora file"), OpenExplorer_Click));
        row.Children.Add(Action(AstraOS.Core.UiText.T("Gestisci profili"), ProfilesNav_Click)); actions.Children.Add(row);
        WorkspacePanel.Children.Add(Card(actions));
        if (_workspace == "stem") AddScientificPanels();
        if (_workspace == "gaming") AddGamingLab();
        if (_workspace == "creator") AddCreatorLab();
        var apps = new StackPanel { Spacing = 10 };
        apps.Children.Add(Label(_workspace == "gaming" ? AstraOS.Core.UiText.T("GIOCHI E LAUNCHER INSTALLATI") : AstraOS.Core.UiText.T("APPLICAZIONI INSTALLATE"), 16));
        apps.Children.Add(Label(AstraOS.Core.UiText.T("Collegamenti del menu Start. Le app portabili e alcuni giochi delle librerie esterne potrebbero non comparire."), 12));
        apps.Children.Add(Action(AstraOS.Core.UiText.T("Rileva applicazioni"), async (_, _) => { await EnsureAppsAsync(true); if (!_windowClosed) RenderWorkspace(); }));
        foreach (var app in _installedApps.Where(a => a.Workspace == _workspace || (_workspace == "core" && _preferences.RecentApps.Contains(a.Id))).Take(16))
            apps.Children.Add(Action(app.Name, (_, _) => LaunchInstalledApp(app)));
        if (!_appsLoaded) apps.Children.Add(Label(AstraOS.Core.UiText.T("Rilevamento su richiesta; nessuna applicazione viene eseguita automaticamente."), 12));
        else if (!_installedApps.Any(a => a.Workspace == _workspace)) apps.Children.Add(Label(AstraOS.Core.UiText.T("Nessuna app di questa categoria trovata. App Hub include il catalogo WinGet opzionale."), 12));
        WorkspacePanel.Children.Add(Card(apps));
        AddProjectsPanel();
        if (_workspace is "gaming" or "creator")
        {
            var performance = new StackPanel { Spacing = 10 };
            performance.Children.Add(Label(_workspace == "gaming" ? AstraOS.Core.UiText.T("SESSIONE E PRESTAZIONI") : AstraOS.Core.UiText.T("MONITORAGGIO RENDERING"), 16));
            _workspaceMetrics = Label(AstraOS.Core.UiText.T("Caricamento metriche reali…")); performance.Children.Add(_workspaceMetrics);
            performance.Children.Add(Label(AstraOS.Core.UiText.T("Eco limita le attività di ASTRA. I profili non modificano le impostazioni energetiche o di sicurezza di Windows."), 12));
            performance.Children.Add(Action(AstraOS.Core.UiText.T("Profilo silenzioso / Eco"), (_, _) => SetQuietMode(true)));
            performance.Children.Add(Action(AstraOS.Core.UiText.T("Profilo bilanciato"), (_, _) => SetQuietMode(false)));
            
            WorkspacePanel.Children.Add(Card(performance));
        }
        _ = RefreshLiveMetricsAsync();
    }

    private void SetQuietMode(bool quiet)
    {
        _preferences.EcoMode = quiet;
        try { _preferenceStore.Save(_preferences); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { StatusText.Text = AstraOS.Core.UiText.T("Profilo non salvato: ") + ex.Message; return; }
        _settingToggles = true; V4EcoToggle.IsOn = EcoModeToggle.IsOn = quiet; _settingToggles = false;
        V4UpdateBannerAnimation(); if (quiet) { Stop3DCore(); StopStudio(); }
        _monitorTimer.Interval = TimeSpan.FromSeconds(quiet ? 15 : 5);
        StatusText.Text = quiet ? AstraOS.Core.UiText.T("Eco attivo · rendering opzionale arrestato · refresh 15 s") : AstraOS.Core.UiText.T("Profilo bilanciato · refresh 5 s");
    }

    private DateTimeOffset? _sessionStarted;
    private void AddSessionControls(StackPanel panel)
    {
        var summary = Label(_sessionStarted.HasValue ? AstraOS.Core.UiText.F("Sessione iniziata alle {0:HH:mm} · durata {1:F0} min", _sessionStarted, (DateTimeOffset.Now - _sessionStarted.Value).TotalMinutes) : AstraOS.Core.UiText.T("Nessuna sessione avviata."));
        panel.Children.Add(summary);
        panel.Children.Add(Action(AstraOS.Core.UiText.T("Avvia sessione"), (_, _) => { _sessionStarted ??= DateTimeOffset.Now; summary.Text = AstraOS.Core.UiText.F("Sessione iniziata alle {0:HH:mm}", _sessionStarted); }));
        panel.Children.Add(Action(AstraOS.Core.UiText.T("Termina sessione"), (_, _) => { summary.Text = _sessionStarted.HasValue ? AstraOS.Core.UiText.F("Durata sessione: {0:F1} min", (DateTimeOffset.Now - _sessionStarted.Value).TotalMinutes) : AstraOS.Core.UiText.T("Nessuna sessione in corso."); _sessionStarted = null; }));
    }

    private void BuildInstalledAppsView()
    {
        InstalledAppsHost.Children.Add(Label(AstraOS.Core.UiText.T("LE TUE APPLICAZIONI"), 20));
        _appSearch = new TextBox { PlaceholderText = AstraOS.Core.UiText.T("Cerca tra le app installate…"), MaxLength = 120 };
        _appSearch.TextChanged += (_, _) => RenderInstalledApps(); InstalledAppsHost.Children.Add(_appSearch);
        _appCategory = new ComboBox { Header = AstraOS.Core.UiText.T("Categoria"), ItemsSource = new[] { "Tutte", "Preferiti", "Recenti", "Core", "STEM", "Gaming", "Creator" }, SelectedIndex = 0 };
        _appCategory.SelectionChanged += (_, _) => RenderInstalledApps(); InstalledAppsHost.Children.Add(_appCategory);
        InstalledAppsHost.Children.Add(Action(AstraOS.Core.UiText.T("Aggiorna app installate"), async (_, _) => await EnsureAppsAsync(true)));
        _appStatus = Label(AstraOS.Core.UiText.T("Rilevamento non ancora eseguito."), 12); InstalledAppsHost.Children.Add(_appStatus);
        _appList = new ListView { MaxHeight = 390, SelectionMode = ListViewSelectionMode.None };
        InstalledAppsHost.Children.Add(_appList);
    }
    private async Task EnsureAppsAsync(bool force = false)
    {
        while (_appsLoading && !_windowClosed) await Task.Delay(50);
        if (_windowClosed || (_appsLoaded && !force)) return;
        _appsLoading = true; if (_appStatus is not null) _appStatus.Text = AstraOS.Core.UiText.T("Ricerca nei collegamenti del menu Start…");
        try
        {
            _installedApps = await Task.Run(() => InstalledAppCatalog.Discover(_windowLifetime.Token), _windowLifetime.Token);
            if (_windowClosed) return;
            _appsLoaded = true; RenderInstalledApps();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { if (!_windowClosed && _appStatus is not null) _appStatus.Text = AstraOS.Core.UiText.T("Rilevamento non disponibile: ") + ex.Message; }
        finally { _appsLoading = false; }
    }
    private void RenderInstalledApps()
    {
        if (_appList is null || _appStatus is null) return;
        _appList.Items.Clear();
        var category = _appCategory?.SelectedIndex ?? 0;
        var apps = _installedApps.Where(a => a.Name.Contains(_appSearch?.Text ?? "", StringComparison.CurrentCultureIgnoreCase) &&
            (category == 0 || category == 1 && _preferences.FavoriteApps.Contains(a.Id) || category == 2 && _preferences.RecentApps.Contains(a.Id) ||
             category >= 3 && a.Workspace == new[] { "core", "stem", "gaming", "creator" }[category - 3])).ToArray();
        foreach (var app in apps.Take(100))
        {
            var row = new Grid { ColumnSpacing = 10, Padding = new(6) };
            row.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star) }); row.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
            row.Children.Add(Action(app.Name + "  ·  " + app.Workspace, (_, _) => LaunchInstalledApp(app)));
            var favorite = Action(_preferences.FavoriteApps.Contains(app.Id) ? "★" : "☆", (_, _) =>
            {
                var previous = _preferences.FavoriteApps.ToList();
                if (!_preferences.FavoriteApps.Remove(app.Id)) _preferences.FavoriteApps.Add(app.Id);
                try { _preferenceStore.Save(_preferences); RenderInstalledApps(); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { _preferences.FavoriteApps = previous; StatusText.Text = AstraOS.Core.UiText.T("Preferito non salvato: ") + ex.Message; }
            });
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(favorite, AstraOS.Core.UiText.T("Preferito: ") + app.Name);
            Grid.SetColumn(favorite, 1); row.Children.Add(favorite); _appList.Items.Add(row);
        }
        _appStatus.Text = AstraOS.Core.UiText.F("{0} app · prime {1} mostrate · filtra per restringere la ricerca · collegamenti menu Start", apps.Length, Math.Min(100, apps.Length));
    }
    private void LaunchInstalledApp(InstalledApplication app)
    {
        try
        {
            if (!_installedApps.Contains(app) || !File.Exists(app.Shortcut)) throw new FileNotFoundException(AstraOS.Core.UiText.T("Collegamento non disponibile; aggiorna l'elenco."));
            Process.Start(new ProcessStartInfo(app.Shortcut) { UseShellExecute = true });
            _preferences.RecentApps.Remove(app.Id); _preferences.RecentApps.Insert(0, app.Id);
            _preferenceStore.Save(_preferences); StatusText.Text = AstraOS.Core.UiText.T("Avvio richiesto: ") + app.Name;
        }
        catch (Exception ex) { StatusText.Text = AstraOS.Core.UiText.T("Avvio app: ") + ex.Message; }
    }

    private void AddProjectsPanel()
    {
        var panel = new StackPanel { Spacing = 10 };
        panel.Children.Add(Label(_workspace == "creator" ? AstraOS.Core.UiText.T("PROGETTI E ASSET") : AstraOS.Core.UiText.T("PROGETTI LOCALI"), 16));
        panel.Children.Add(Action(AstraOS.Core.UiText.T("Collega cartella progetto…"), async (_, _) =>
        {
            try
            {
                var workspace = _workspace;
                var picker = new FolderPicker(); picker.FileTypeFilter.Add("*");
                WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(this));
                var folder = await picker.PickSingleFolderAsync(); if (folder is null || _windowClosed) return;
                _preferences.Projects.Add(new(folder.Name, folder.Path, workspace));
                _preferenceStore.Save(_preferences); RenderWorkspace();
            }
            catch (Exception ex) { if (!_windowClosed) StatusText.Text = AstraOS.Core.UiText.T("Progetto: ") + ex.Message; }
        }));
        foreach (var project in _preferences.Projects.Where(p => p.Workspace == _workspace))
            panel.Children.Add(Action(project.Name + "  →  " + project.Directory, (_, _) =>
            {
                try
                {
                    if (!Directory.Exists(project.Directory)) throw new DirectoryNotFoundException(AstraOS.Core.UiText.T("Cartella non più disponibile."));
                    var psi = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe")) { UseShellExecute = true };
                    psi.ArgumentList.Add(project.Directory); Process.Start(psi);
                }
                catch (Exception ex) { StatusText.Text = AstraOS.Core.UiText.T("Apertura progetto: ") + ex.Message; }
            }));
        if (!_preferences.Projects.Any(p => p.Workspace == _workspace)) panel.Children.Add(Label(AstraOS.Core.UiText.T("Nessun progetto collegato. Scegli una cartella esistente; i file restano nella posizione originale."), 12));
        WorkspacePanel.Children.Add(Card(panel));
    }
}
