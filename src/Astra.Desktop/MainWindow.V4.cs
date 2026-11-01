using System.Diagnostics;
using System.Numerics;
using AstraOS.Core;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Input;
using Windows.UI;
using Windows.Graphics;

namespace AstraOS.Desktop;

public sealed partial class MainWindow
{
    private readonly EncryptedTaskStore _v4TaskStore = new();
    private List<AstraTaskItem> _v4Tasks = new();
    private readonly DispatcherTimer _v4ClockTimer = new() { Interval = TimeSpan.FromMinutes(1) };
    private bool _v4BannerActive;
    // Keep the exact WinUI animation instance: UIElement.StopAnimation expects
    // ICompositionAnimationBase, not the string name of a property.
    private Vector3KeyFrameAnimation? _v4BannerAnimation;
    private string _v4LastPage = "home";

    private void InitializeV4()
    {
        // MainWindow remains an ordinary asInvoker Windows application; Explorer is not replaced.
        ApplyV4Palette(_preferences.Palette);
        V4LoadThemeInputs(_preferences.Palette);
        _settingToggles = true;
        V4EcoToggle.IsOn = _preferences.EcoMode;
        V4MotionToggle.IsOn = _preferences.ReducedMotion;
        V4DockToggle.IsOn = _preferences.ShowDock;
        _settingToggles = false;
        try { _v4Tasks = _v4TaskStore.Load(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.Cryptography.CryptographicException or InvalidDataException)
        { StatusText.Text = AstraOS.Core.UiText.T("Attività cifrate non leggibili: ") + ex.Message; }
        V4RenderTasks();
        V4RenderActivityChart();
        V4Clock.Text = DateTime.Now.ToString("ddd dd MMM · HH:mm");
        _v4ClockTimer.Tick += (_, _) => V4Clock.Text = DateTime.Now.ToString("ddd dd MMM · HH:mm");
        _v4ClockTimer.Start();
        BannerArtworkImage.Loaded += (_, _) => V4UpdateBannerAnimation();
        Activated += (_, e) =>
        {
            if (e.WindowActivationState == WindowActivationState.Deactivated) V4StopBanner();
            else V4UpdateBannerAnimation();
        };
        Closed += (_, _) => { _v4ClockTimer.Stop(); V4StopBanner(); };
        try { AppWindow.Resize(new SizeInt32(1560, 950)); }
        catch (Exception) { /* Size can be constrained by platform/window manager. */ }
    }

    private void V4OnNavigate(string page)
    {
        _v4LastPage = page;
        PageHeading.Visibility = page == "home" ? Visibility.Collapsed : Visibility.Visible;
        SearchSuggestions.Visibility = string.IsNullOrWhiteSpace(HorizonSearchBox.Text) ? Visibility.Collapsed : Visibility.Visible;
        ThemePanel.Visibility = page == "theme" ? Visibility.Visible : Visibility.Collapsed;
        if (page == "theme") { PageTitle.Text = AstraOS.Core.UiText.T("Personalizza ASTRA"); PageSubtitle.Text = AstraOS.Core.UiText.T("Cinque colori · temi persistenti · accessibilità"); }
        V4UpdateBannerAnimation();
        // Runtime brush updates also keep old module panels in the same color family.
        var selected = (SolidColorBrush)AppRoot.Resources["AstraBlue"];
        foreach (var (button, route) in new (Button, string)[]
        {
            (HomeNav,"home"),(ProfilesNav,"profiles"),(CatalogNav,"catalog"),
            (DiagnosticNav,"diagnostics"),(SecurityNav,"security"),(BrainNav,"brain"),
            (PluginsNav,"plugins"),(OrbitNav,"orbit"),(UbuntuNav,"ubuntu"),
            (IsoNav,"iso"),(ThemeNav,"theme")
        }) button.Background = page == route ? selected : new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
    }

    private void ThemeNav_Click(object sender, RoutedEventArgs e) => Navigate("theme");
    private void V4SearchAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        HorizonSearchBox.Focus(FocusState.Programmatic);
        args.Handled = true;
    }
    private void V4FocusSearch_Click(object sender, RoutedEventArgs e) => HorizonSearchBox.Focus(FocusState.Programmatic);

    internal static Color ColorFromHex(string hex)
        => Color.FromArgb(255, Convert.ToByte(hex.Substring(1,2),16),
                                Convert.ToByte(hex.Substring(3,2),16), Convert.ToByte(hex.Substring(5,2),16));

    private void ApplyV4Palette(AstraPalette input)
    {
        var p = AstraPalette.Sanitize(input);
        foreach (var (resource, hex) in new (string, string)[]
        {
            ("AstraNavy",p.Navy), ("AstraIce",p.Ice), ("AstraBlue",p.Blue),
            ("AstraDeep",p.Deep), ("AstraSnow",p.Snow),
            ("AstraInk",AstraPalette.ContrastingText(p.Snow)),
            ("AstraIceText",AstraPalette.ContrastingText(p.Ice)),
            ("AstraTextLight",AstraPalette.ContrastingText(p.Navy)),
            ("AstraDeepText",AstraPalette.ContrastingText(p.Deep)),
            ("AstraAccentText",AstraPalette.ContrastingText(p.Blue))
        })
        {
            // Mutate the existing shared brush: every StaticResource consumer updates live.
            if (AppRoot.Resources[resource] is SolidColorBrush b) b.Color = ColorFromHex(hex);
        }
        foreach (var key in new[] { "AccentFillColorDefaultBrush", "AccentFillColorSecondaryBrush", "AccentFillColorTertiaryBrush",
            "ToggleSwitchFillOnBrush", "ToggleSwitchFillOnPointerOverBrush", "ToggleSwitchFillOnPressedBrush",
            "CheckBoxCheckBackgroundFillCheckedBrush", "ProgressBarForeground" })
            if (AppRoot.Resources[key] is SolidColorBrush accentBrush) accentBrush.Color = ColorFromHex(p.Blue);
        // Generated background art is blue-centric. Suppress it when using a custom palette.
        AppRoot.Background = (SolidColorBrush)AppRoot.Resources["AstraNavy"];
        ApplyArtOpacity();
        SendStudioScene();
        ApplyOrbitPalette();
        RefreshThemeCursors();
        V4OnNavigate(_v4LastPage);
    }

    private void V4LoadThemeInputs(AstraPalette p)
    {
        V4NavyInput.Text = p.Navy; V4IceInput.Text = p.Ice;
        V4BlueInput.Text = p.Blue; V4DeepInput.Text = p.Deep; V4SnowInput.Text = p.Snow;
    }

    private void V4ApplyTheme_Click(object sender, RoutedEventArgs e)
    {
        if (!AstraPalette.TryCreate(V4NavyInput.Text, V4IceInput.Text, V4BlueInput.Text,
                V4DeepInput.Text, V4SnowInput.Text, out var next))
        { V4ThemeStatus.Text = AstraOS.Core.UiText.T("Formato non valido: usa cinque colori nel formato #RRGGBB."); return; }
        var previous = _preferences.Palette;
        try
        {
            _preferences.Palette = next;
            ApplyV4Palette(next);
            _preferenceStore.Save(_preferences);
            V4ThemeStatus.Text = AstraOS.Core.UiText.T("Palette salvata localmente e applicata senza riavvio.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _preferences.Palette = previous; ApplyV4Palette(previous);
            V4ThemeStatus.Text = AstraOS.Core.UiText.T("Salvataggio fallito: ") + ex.Message;
        }
    }
    private void V4ResetTheme_Click(object sender, RoutedEventArgs e)
    {
        V4LoadThemeInputs(AstraPalette.Default);
        V4ApplyTheme_Click(sender, e);
    }

    private void V4PerformanceToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_settingToggles || _preferences is null) return;
        var old = (_preferences.EcoMode, _preferences.ReducedMotion, _preferences.ShowDock);
        _preferences.EcoMode = V4EcoToggle.IsOn;
        _preferences.ReducedMotion = V4MotionToggle.IsOn;
        _preferences.ShowDock = V4DockToggle.IsOn;
        try
        {
            _preferenceStore.Save(_preferences);
            _settingToggles = true;
            EcoModeToggle.IsOn = _preferences.EcoMode;
            ReduceMotionToggle.IsOn = _preferences.ReducedMotion;
            DockVisibilityToggle.IsOn = _preferences.ShowDock;
            _settingToggles = false;
            HorizonDock.Visibility = _preferences.ShowDock ? Visibility.Visible : Visibility.Collapsed;
            V4UpdateBannerAnimation();
            if (_preferences.EcoMode || _preferences.ReducedMotion) { Stop3DCore(); StopStudio(); }
            _monitorTimer.Interval = TimeSpan.FromSeconds(_preferences.EcoMode ? 15 : 5);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            (_preferences.EcoMode,_preferences.ReducedMotion,_preferences.ShowDock) = old;
            V4ThemeStatus.Text = AstraOS.Core.UiText.T("Preferenze non salvate: ") + ex.Message;
        }
    }

    private void V4StopBanner()
    {
        if (!_v4BannerActive) return;
        try
        {
            if (_v4BannerAnimation is not null)
                BannerArtworkImage.StopAnimation(_v4BannerAnimation);
        }
        catch (Exception)
        {
            // A detached/unloaded element must not crash the desktop.
        }
        finally
        {
            _v4BannerAnimation = null;
            _v4BannerActive = false;
        }
    }
    private void V4UpdateBannerAnimation()
    {
        if (BannerArtworkImage is null || _preferences is null) return;
        UpdateAnimatedImages();
        if (!MotionAllowed) { V4StopBanner(); ResetMotion(); return; }
        if (_preferences.EcoMode || _preferences.ReducedMotion || !new Windows.UI.ViewManagement.UISettings().AnimationsEnabled || _v4LastPage != "home")
        { V4StopBanner(); return; }
        if (_v4BannerActive) return;
        try
        {
            var compositor = CompositionTarget.GetCompositorForCurrentThread();
            var animation = compositor.CreateVector3KeyFrameAnimation();
            animation.Target = "Translation";
            animation.InsertKeyFrame(0f, Vector3.Zero);
            animation.InsertKeyFrame(0.5f, new Vector3(-14f, 6f, 0f));
            animation.InsertKeyFrame(1f, Vector3.Zero);
            animation.Duration = TimeSpan.FromSeconds(20);
            animation.IterationBehavior = AnimationIterationBehavior.Forever;
            BannerArtworkImage.StartAnimation(animation);
            _v4BannerAnimation = animation;
            _v4BannerActive = true;
        }
        catch (Exception)
        {
            // Composition failures must never block the application.
            V4StopBanner();
        }
    }

    private async void V4RefreshStats_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button) button.IsEnabled = false;
        try
        {
            await RefreshLiveMetricsAsync(force: true);
        }
        catch (Exception ex) { StatusText.Text = AstraOS.Core.UiText.T("Diagnostica non disponibile: ") + ex.Message; }
        finally { if (sender is Button finishButton) finishButton.IsEnabled = true; }
    }

    private void V4AddTask_Click(object sender, RoutedEventArgs e)
    {
        var title = V4NewTask.Text.Trim();
        if (title.Length == 0 || title.Length > 120 || _v4Tasks.Count >= 100) return;
        var item = new AstraTaskItem { Text = title };
        _v4Tasks.Add(item);
        try { _v4TaskStore.Save(_v4Tasks); V4NewTask.Text = ""; V4RenderTasks(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.Cryptography.CryptographicException)
        { _v4Tasks.Remove(item); StatusText.Text = AstraOS.Core.UiText.T("Attività non salvata: ") + ex.Message; }
    }
    private void V4TaskChecked(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox cb || cb.Tag is not string id) return;
        var task = _v4Tasks.FirstOrDefault(t => t.Id == id);
        if (task is null) return;
        var previous = task.CompletedUtc;
        task.CompletedUtc = cb.IsChecked == true ? DateTimeOffset.UtcNow : null;
        try { _v4TaskStore.Save(_v4Tasks); V4RenderActivityChart(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.Cryptography.CryptographicException)
        { task.CompletedUtc = previous; cb.IsChecked = task.Completed; StatusText.Text = AstraOS.Core.UiText.T("Modifica non salvata: ") + ex.Message; }
    }
    private void V4RemoveTask_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button b || b.Tag is not string id) return;
        var existing = _v4Tasks.FirstOrDefault(t => t.Id == id);
        if (existing is null) return;
        _v4Tasks.Remove(existing);
        try { _v4TaskStore.Save(_v4Tasks); V4RenderTasks(); V4RenderActivityChart(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.Cryptography.CryptographicException)
        { _v4Tasks.Add(existing); V4RenderTasks(); StatusText.Text = AstraOS.Core.UiText.T("Eliminazione non salvata: ") + ex.Message; }
    }
    private void V4RenderTasks()
    {
        V4TaskItems.Children.Clear();
        V4TaskEmpty.Visibility = _v4Tasks.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        foreach (var t in _v4Tasks)
        {
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var cb = new CheckBox { Content = new TextBlock { Text = t.Text, TextWrapping = TextWrapping.Wrap, MaxWidth = 200 }, IsChecked = t.Completed, Tag = t.Id,
                Foreground = (SolidColorBrush)AppRoot.Resources["AstraInk"] };
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(cb, t.Text);
            cb.Click += V4TaskChecked;
            row.Children.Add(cb);
            var remove = new Button { Content = "×", Tag = t.Id,
                Padding = new Thickness(8,2,8,2) };
            remove.Click += V4RemoveTask_Click;
            Grid.SetColumn(remove, 1); row.Children.Add(remove);
            V4TaskItems.Children.Add(row);
        }
    }
    private void V4RenderActivityChart()
    {
        V4WeekChart.Children.Clear();
        var today = DateTimeOffset.Now.Date;
        int[] completed = Enumerable.Range(0, 7).Select(i => _v4Tasks.Count(t =>
            t.CompletedUtc.HasValue && t.CompletedUtc.Value.ToLocalTime().Date == today.AddDays(i - 6))).ToArray();
        int max = Math.Max(1,completed.Max());
        for (int i=0;i<7;i++)
        {
            var bar = new Border { Height = 80.0*completed[i]/max,
                Background = (SolidColorBrush)AppRoot.Resources["AstraBlue"],
                VerticalAlignment = VerticalAlignment.Bottom, CornerRadius = new CornerRadius(4) };
            Grid.SetColumn(bar,i); V4WeekChart.Children.Add(bar);
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(bar, AstraOS.Core.UiText.F("{0:ddd dd}: {1} attività completate", today.AddDays(i-6), completed[i]));
        }
        V4WeekLabels.Text = string.Join("     ", Enumerable.Range(0,7).Select(i => today.AddDays(i-6).ToString("ddd")));
    }
}
