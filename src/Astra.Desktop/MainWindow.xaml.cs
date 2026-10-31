using System.Diagnostics;
using AstraOS.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI;

namespace AstraOS.Desktop;

public sealed partial class MainWindow : Window
{
    private readonly PreferenceStore _preferenceStore = new();
    private readonly ProfileCatalog _profileCatalog;
    private AstraPreferences _preferences;
    private bool _settingToggles;

    public MainWindow()
    {
        StartupDiagnostics.Record("MainWindow.Xaml.InitializeComponent.begin");
        InitializeComponent();
        StartupDiagnostics.Record("MainWindow.Xaml.InitializeComponent.complete");
        _profileCatalog = ProfileCatalog.Load(Path.Combine(AppContext.BaseDirectory, "profiles"));
        _preferences = _preferenceStore.Load();
        ApplyHardwareDefaults();
        _settingToggles = true;
        StemToggle.IsOn = _preferences.SelectedProfiles.Contains("stem");
        GamingToggle.IsOn = _preferences.SelectedProfiles.Contains("gaming");
        CreatorToggle.IsOn = _preferences.SelectedProfiles.Contains("creator");
        EcoModeToggle.IsOn = _preferences.EcoMode;
        ReduceMotionToggle.IsOn = _preferences.ReducedMotion;
        DockVisibilityToggle.IsOn = _preferences.ShowDock;
        HorizonDock.Visibility = _preferences.ShowDock ? Visibility.Visible : Visibility.Collapsed;
        ApplyFocusMode(_preferences.FocusMode);
        OrbitFpsCombo.SelectedIndex = _preferences.OrbitFpsCap switch { 15 => 0, 60 => 2, _ => 1 };
        _settingToggles = false;
        RenderPackages();
        InitializeExtensions();
        InitializeV4();
        InitializeProductivity();
        InitializeStudyProfile();
        InitializeStudio();
        InitializeMotion();
        InitializePersonalArt();
        InitializeWindowMode();
        InitializeWidgets();
        InitializeWindowsTools();
        InitializeAssistant();
        InitializeBrainDocuments();
        InitializeAssistantTools();
        InitializeSystemHub();
        InitializeFileActions();
        InitializeMultitasking();
        InitializeProfileLabs();
        InitializeThemeCursors();
        InitializeInterfaceLanguage();
        InitializeExperience();
        InitializeResponsiveBanner();
        InitializeRunningAppsDock();
        try{AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory,"Assets","astra.ico"));}catch(Exception){}
        LocalizeXaml();
        AppRoot.Loaded+=(_,_)=>{LocalizeXaml();RefreshAssistantIdentity();};
        Navigate("home");
    }

    private void Navigate(string page)
    {
        if(page=="assistant")page="brain";
        if(_systemHubPanel is not null)_systemHubPanel.Visibility=page=="system"?Visibility.Visible:Visibility.Collapsed;
        if(page != "stem") StopStudio();
        ExtensionNavigation(page);
        MultitaskPanel.Visibility=page=="multitasking"?Visibility.Visible:Visibility.Collapsed;
        if(page=="multitasking")RefreshWindowList();
        AssistantPanel.Visibility=page=="assistant"?Visibility.Visible:Visibility.Collapsed;
        WindowsToolsPanel.Visibility=page=="windows-tools"?Visibility.Visible:Visibility.Collapsed;
        if(page=="windows-tools"&&_windowsTools.Count==0)_=RefreshWindowsToolsAsync();
        WidgetsPanel.Visibility=page=="widgets"?Visibility.Visible:Visibility.Collapsed;
        if(page=="widgets")RenderWidgets();
        UbuntuPanel.Visibility = page == "ubuntu" ? Visibility.Visible : Visibility.Collapsed;
        HomePanel.Visibility = page == "home" ? Visibility.Visible : Visibility.Collapsed;
        ProfilesPanel.Visibility = page == "profiles" ? Visibility.Visible : Visibility.Collapsed;
        CatalogPanel.Visibility = page == "catalog" ? Visibility.Visible : Visibility.Collapsed;
        DiagnosticPanel.Visibility = page == "diagnostics" ? Visibility.Visible : Visibility.Collapsed;
        SecurityPanel.Visibility = page == "security" ? Visibility.Visible : Visibility.Collapsed;
        PageTitle.Text = page switch
        {
            "multitasking" => AstraOS.Core.UiText.T("Multitasking e sessioni"),
            "assistant" => AstraOS.Core.UiText.T("Assistente ASTRA"),
            "windows-tools" => AstraOS.Core.UiText.T("Strumenti Windows"),
            "widgets" => AstraOS.Core.UiText.T("Widget"),
            "profiles" => AstraOS.Core.UiText.T("Profili ASTRA"),
            "catalog" => AstraOS.Core.UiText.T("ASTRA App Hub"),
            "diagnostics" => AstraOS.Core.UiText.T("Performance Center"),
            "security" => AstraOS.Core.UiText.T("Security Center"),
            "brain" => AstraOS.Core.UiText.T("ASTRA Second Brain"),
            "plugins" => AstraOS.Core.UiText.T("Plugin SDK"),
            "orbit" => AstraOS.Core.UiText.T("Desktop 3D / Orbital"),
            "iso" => AstraOS.Core.UiText.T("ISO Image Builder"),
            "ubuntu" => AstraOS.Core.UiText.T("Ubuntu Lab · Linux dentro Windows"),
            _ => AstraOS.Core.UiText.T("Panoramica")
        };
        PageSubtitle.Text = page switch
        {
            "profiles" => AstraOS.Core.UiText.T("Quattro ambienti, un solo sistema."),
            "catalog" => AstraOS.Core.UiText.T("Applicazioni opzionali, senza installazioni silenziose."),
            "diagnostics" => AstraOS.Core.UiText.T("Monitoraggio leggero e manuale."),
            "security" => AstraOS.Core.UiText.T("La sicurezza di Windows rimane al suo posto."),
            "brain" => AstraOS.Core.UiText.T("Modelli locali, note private e ricerca sul dispositivo."),
            "plugins" => AstraOS.Core.UiText.T("Estensioni approvate dall'utente, senza caricamento in-process."),
            "orbit" => AstraOS.Core.UiText.T("Esperienza grafica opzionale, GPU on-demand."),
            "iso" => AstraOS.Core.UiText.T("Personalizza media ufficiali ottenuti da te."),
            "ubuntu" => AstraOS.Core.UiText.T("Bash, APT e strumenti Linux su richiesta, senza sostituire Windows."),
            _ => AstraOS.Core.UiText.T("Il tuo nuovo ambiente Windows.")
        };
        var activeBrush = new SolidColorBrush(Color.FromArgb(255, 27, 59, 100));
        var idleBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        HomeNav.Background = page == "home" ? activeBrush : idleBrush;
        ProfilesNav.Background = page == "profiles" ? activeBrush : idleBrush;
        CatalogNav.Background = page == "catalog" ? activeBrush : idleBrush;
        DiagnosticNav.Background = page == "diagnostics" ? activeBrush : idleBrush;
        SecurityNav.Background = page == "security" ? activeBrush : idleBrush;
        UbuntuNav.Background = page == "ubuntu" ? activeBrush : idleBrush;
        V4OnNavigate(page);
        OnProductivityNavigate(page);
        if(page=="system"){PageTitle.Text=UiText.T("Centro di controllo");PageSubtitle.Text=UiText.T("Funzioni quotidiane · strumenti di sistema · produttività");}
        RefreshAssistantIdentity();
        ContentScroller.ChangeView(null,0,null,true);
        AnimateNavigation(page);
        DispatcherQueue.TryEnqueue(()=>LocalizeCombos(AppRoot));
    }

    private void HomeNav_Click(object sender, RoutedEventArgs e) => Navigate("home");
    private void ProfilesNav_Click(object sender, RoutedEventArgs e) => Navigate("profiles");
    private void CatalogNav_Click(object sender, RoutedEventArgs e) { RenderPackages(); Navigate("catalog"); }
    private void DiagnosticNav_Click(object sender, RoutedEventArgs e) => Navigate("diagnostics");
    private void SecurityNav_Click(object sender, RoutedEventArgs e) => Navigate("security");

    private void ProfileToggled(object sender, RoutedEventArgs e)
    {
        if (_settingToggles || _preferences is null) return;
        var old = _preferences.SelectedProfiles.ToList();
        var next = new List<string> { "core" };
        if (StemToggle.IsOn) next.Add("stem");
        if (GamingToggle.IsOn) next.Add("gaming");
        if (CreatorToggle.IsOn) next.Add("creator");
        try
        {
            _preferences.SelectedProfiles = next;
            _preferenceStore.Save(_preferences);
            RenderPackages();
            StatusText.Text = AstraOS.Core.UiText.T("Profili salvati localmente · nessuna applicazione installata automaticamente");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _preferences.SelectedProfiles = old;
            _settingToggles = true;
            StemToggle.IsOn = old.Contains("stem");
            GamingToggle.IsOn = old.Contains("gaming");
            CreatorToggle.IsOn = old.Contains("creator");
            _settingToggles = false;
            StatusText.Text = AstraOS.Core.UiText.T("Salvataggio non riuscito: ") + ex.Message;
        }
    }

    private void PerformanceToggled(object sender, RoutedEventArgs e) => SavePerformancePreferences();
    private void OrbitFpsSelectionChanged(object sender, SelectionChangedEventArgs e) => SavePerformancePreferences();
    private void SavePerformancePreferences()
    {
        if (_settingToggles || _preferences is null) return;
        var old = (_preferences.EcoMode, _preferences.ReducedMotion, _preferences.OrbitFpsCap);
        _preferences.EcoMode = EcoModeToggle.IsOn;
        _preferences.ReducedMotion = ReduceMotionToggle.IsOn;
        _preferences.OrbitFpsCap = OrbitFpsCombo.SelectedIndex switch { 0 => 15, 2 => 60, _ => 30 };
        try
        {
            _preferenceStore.Save(_preferences);
            PerformanceModeStatus.Text = AstraOS.Core.UiText.F("Impostazioni locali salvate · 3D: {0} FPS effettivi", PerformancePolicy.EffectiveOrbitFps(_preferences));
            _settingToggles = true; V4EcoToggle.IsOn = _preferences.EcoMode; V4MotionToggle.IsOn = _preferences.ReducedMotion; _settingToggles = false;
            V4UpdateBannerAnimation(); Stop3DCore(); StopStudio();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            (_preferences.EcoMode, _preferences.ReducedMotion, _preferences.OrbitFpsCap) = old;
            PerformanceModeStatus.Text = AstraOS.Core.UiText.T("Impossibile salvare: ") + ex.Message;
        }
    }

    private void RenderPackages()
    {
        PackageList.Children.Clear();
        var packages = _profileCatalog.PackagesFor(_preferences.SelectedProfiles);
        PackageCount.Text = AstraOS.Core.UiText.F("{0} applicazioni disponibili per i profili attivi", packages.Count);
        if (packages.Count == 0)
        {
            PackageList.Children.Add(new TextBlock {
                Text = AstraOS.Core.UiText.T("Attiva STEM, Gaming o Creator per visualizzare le applicazioni consigliate."),
                Foreground = Token("AstraIce"), TextWrapping = TextWrapping.Wrap
            });
            return;
        }
        foreach (var package in packages)
        {
            var container = new Border
            {
                Background = Token("AstraDeep"), BorderBrush = Token("AstraBlue"), BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12), Padding = new Thickness(18)
            };
            var grid = new Grid { ColumnSpacing = 16 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var text = new StackPanel { Spacing = 6 };
            text.Children.Add(new TextBlock { Text = package.Name, FontSize = 17 });
            text.Children.Add(new TextBlock { Text = $"{package.Category} · {package.Description} · {package.Id}",
                Foreground = Token("AstraDeepText"), TextWrapping = TextWrapping.Wrap, FontSize = 12 });
            var button = new Button { Content = AstraOS.Core.UiText.T("Installa con WinGet"), Tag = package.Id,
                VerticalAlignment = VerticalAlignment.Center, CornerRadius = new CornerRadius(8) };
            button.Click += InstallPackage_Click;
            Grid.SetColumn(button, 1);
            grid.Children.Add(text);
            grid.Children.Add(button);
            container.Child = grid;
            PackageList.Children.Add(container);
        }
    }

    private async void InstallPackage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string packageId) return;
        var package = CuratedCatalog.Get(packageId);
        var command = CommandSafety.DisplayWingetCommand(packageId);
        var dialog = new ContentDialog
        {
            XamlRoot = AppRoot.XamlRoot,
            Title = AstraOS.Core.UiText.T("Conferma installazione: ") + package.Name,
            Content = new TextBlock {
                Text = AstraOS.Core.UiText.T("ASTRA avvierà WinGet per questo pacchetto, senza accettare automaticamente contratti. ") +
                       AstraOS.Core.UiText.T("Conferma eventuali richieste del gestore pacchetti.\n\nComando: ") + command,
                TextWrapping = TextWrapping.Wrap
            },
            PrimaryButtonText = AstraOS.Core.UiText.T("Avvia WinGet"),
            SecondaryButtonText = AstraOS.Core.UiText.T("Copia comando"),
            CloseButtonText = AstraOS.Core.UiText.T("Annulla"),
            DefaultButton = ContentDialogButton.Close
        };
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Secondary)
        {
            var data = new DataPackage();
            data.SetText(command);
            Clipboard.SetContent(data);
            StatusText.Text = AstraOS.Core.UiText.T("Comando copiato negli appunti");
        }
        else if (result == ContentDialogResult.Primary)
        {
            button.IsEnabled = false;
            StatusText.Text = AstraOS.Core.UiText.T("WinGet in esecuzione · completa le eventuali conferme nella sua finestra");
            try
            {
                var code = await WinGetLauncher.InstallAsync(packageId, _windowLifetime.Token);
                if (_windowClosed) return;
                StatusText.Text = code == 0 ? AstraOS.Core.UiText.T("WinGet completato con successo. Aggiornamento app installate…") : AstraOS.Core.UiText.F("WinGet terminato con errore 0x{0:X8}. Nessuna installazione confermata.", code);
                if (code == 0) await EnsureAppsAsync(true);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (!_windowClosed) StatusText.Text = AstraOS.Core.UiText.T("Installazione: ") + ex.Message; }
            finally { if (!_windowClosed) button.IsEnabled = true; }
        }
    }

    private async void RefreshDiagnostics_Click(object sender, RoutedEventArgs e)
    {
        RefreshButton.IsEnabled = false;
        StatusText.Text = AstraOS.Core.UiText.T("Lettura diagnostica in corso…");
        try
        {
            var snapshot = await Task.Run(SystemSnapshot.Capture);
            AppCpuValue.Text = AstraOS.Core.UiText.T("Misurazione…");
            using (var myself = Process.GetCurrentProcess())
            {
                var cpuStart = myself.TotalProcessorTime;
                var start = Stopwatch.GetTimestamp();
                await Task.Delay(350);
                myself.Refresh();
                var duration = Stopwatch.GetElapsedTime(start).TotalSeconds;
                var cpuDelta = (myself.TotalProcessorTime - cpuStart).TotalSeconds;
                var percent = duration <= 0 ? 0 : 100 * cpuDelta / (duration * Environment.ProcessorCount);
                AppCpuValue.Text = AstraOS.Core.UiText.F("{0:F2}% della CPU totale (circa)", Math.Clamp(percent, 0, 100));
            }
            AppRamValue.Text = snapshot.AppMemoryMb + AstraOS.Core.UiText.T(" MB");
            RamValue.Text = snapshot.PhysicalMemoryMb > 0 ? snapshot.PhysicalMemoryMb + AstraOS.Core.UiText.T(" MB") : AstraOS.Core.UiText.T("Non disponibile");
            ArchitectureValue.Text = snapshot.Architecture;
            ThreadsValue.Text = snapshot.CpuThreads;
            OsValue.Text = snapshot.Os;
            StatusText.Text = AstraOS.Core.UiText.T("Diagnostica aggiornata su richiesta · nessun monitoraggio persistente");
        }
        catch (Exception ex)
        {
            StatusText.Text = AstraOS.Core.UiText.T("Errore diagnostica: ") + ex.Message;
        }
        finally { RefreshButton.IsEnabled = true; }
    }

    private void OpenExplorer_Click(object sender, RoutedEventArgs e) => Navigate("files");
    private void OpenWindowsSettings_Click(object sender, RoutedEventArgs e) => OpenSystemSettings("ms-settings:");
    private void OpenInstalledApps_Click(object sender, RoutedEventArgs e) => OpenSystemSettings("ms-settings:appsfeatures");

    private async void SecurityAudit_Click(object sender, RoutedEventArgs e)
    {
        SecurityAuditButton.IsEnabled = false;
        SecurityAuditText.Text = AstraOS.Core.UiText.T("Lettura sicurezza Windows in corso…");
        try { SecurityAuditText.Text = await SecurityInspector.ReadAsync(); }
        catch (Exception ex) { SecurityAuditText.Text = AstraOS.Core.UiText.T("Audit non disponibile: ") + ex.Message; }
        finally { SecurityAuditButton.IsEnabled = true; }
    }

    private void OpenWindowsSecurity_Click(object sender, RoutedEventArgs e) => OpenSystemSettings("ms-settings:windowsdefender");
    private void OpenWindowsUpdate_Click(object sender, RoutedEventArgs e) => OpenSystemSettings("ms-settings:windowsupdate");

    private void OpenSystemSettings(string uri)
    {
        try { Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true }); }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            StatusText.Text = AstraOS.Core.UiText.T("Impossibile aprire Impostazioni Windows: ") + ex.Message;
        }
    }

    private static SolidColorBrush Brush(string hex)
    {
        // Hex strings here are fixed literals in source, not user input.
        var bytes = Convert.FromHexString(hex.TrimStart('#'));
        return new SolidColorBrush(Color.FromArgb(255, bytes[0], bytes[1], bytes[2]));
    }
}
