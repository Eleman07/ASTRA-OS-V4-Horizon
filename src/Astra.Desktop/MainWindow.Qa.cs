using System.Text.Json;
using AstraOS.Core;
using AstraOS.Core.SecondBrain;
using Microsoft.UI.Xaml;

namespace AstraOS.Desktop;

public sealed partial class MainWindow
{
    // Explicit opt-in integration harness. Never runs during an ordinary launch.
    // The build script must also supply a separate ASTRA_DATA_DIRECTORY.
    private async Task RunDesktopSmokeAsync()
    {
        var checks = new List<object>();
        void Check(string name, bool passed, string? detail = null) => checks.Add(new { name, status = passed ? "PASS" : "FAIL", detail });
        try
        {
            StartupDiagnostics.Record("DesktopSmoke.begin");
            Check("Dashboard visible", HomePanel.Visibility == Visibility.Visible && AppRoot.ActualWidth > 0);
            Check("Orbital is lazy", _orbitView is null && OrbitViewHost.Children.Count == 0);
            Check("DPI observed", true, $"RasterizationScale={AppRoot.XamlRoot.RasterizationScale}; viewport={AppRoot.ActualWidth:F0}x{AppRoot.ActualHeight:F0}");
            var windowedSize = AppWindow.Size;
            _startFullScreenToggle!.IsOn = true;
            await Task.Delay(250);
            Check("Fullscreen presenter", AppWindow.Presenter.Kind == Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);
            var display = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(AppWindow.Id, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
            Check("Fullscreen covers entire monitor", AppWindow.Size.Width == display.OuterBounds.Width && AppWindow.Size.Height == display.OuterBounds.Height,
                $"window={AppWindow.Size.Width}x{AppWindow.Size.Height}; monitor={display.OuterBounds.Width}x{display.OuterBounds.Height}");
            Check("Fullscreen startup preference persists", _preferenceStore.Load().StartFullScreen);
            ToggleFullScreen();
            Check("Fullscreen exit preserves startup preference", AppWindow.Presenter.Kind == Microsoft.UI.Windowing.AppWindowPresenterKind.Overlapped && _preferences.StartFullScreen);
            _startFullScreenToggle.IsOn = false;
            Check("Fullscreen startup can be disabled", !_preferenceStore.Load().StartFullScreen);
            Check("Original window size restored", AppWindow.Size.Width == windowedSize.Width && AppWindow.Size.Height == windowedSize.Height);
            Check("QA cannot alter Windows login startup", !_startAtLoginToggle!.IsEnabled);
            foreach (var route in new[] { "files", "core", "stem", "gaming", "creator", "profiles", "catalog", "diagnostics", "brain", "plugins", "ubuntu", "iso", "theme" })
            {
                Navigate(route);
                Check("Navigate " + route, _v4LastPage == route && !string.IsNullOrWhiteSpace(PageTitle.Text));
            }
            await EnsureAppsAsync(true);
            Check("Installed applications", _appsLoaded, $"{_installedApps.Count} actual Start menu shortcuts");
            if (_installedApps.Count > 0)
            {
                _preferences.FavoriteApps = new() { _installedApps[0].Id };
                _preferenceStore.Save(_preferences);
                Check("Favorite persistence", _preferenceStore.Load().FavoriteApps.Contains(_installedApps[0].Id));
            }
            V4NavyInput.Text = "#102440"; V4IceInput.Text = "#C6DDF0"; V4BlueInput.Text = "#568CB5";
            V4DeepInput.Text = "#22496C"; V4SnowInput.Text = "#F0F5FA";
            V4ApplyTheme_Click(this, new RoutedEventArgs());
            Check("Theme applied live", Token("AstraNavy").Color == ColorFromHex("#102440"));
            Check("Theme persisted", _preferenceStore.Load().Palette.Navy == "#102440");
            V4ResetTheme_Click(this, new RoutedEventArgs());
            Check("Theme reset", _preferences.Palette.IsDefault && Token("AstraNavy").Color == ColorFromHex("#0A1931"));
            V4DockToggle.IsOn = false;
            Check("Dock hidden", HorizonDock.Visibility == Visibility.Collapsed && !_preferenceStore.Load().ShowDock);
            V4DockToggle.IsOn = true;
            SetQuietMode(true);
            Check("Eco enabled", _preferences.EcoMode && _monitorTimer.Interval.TotalSeconds == 15);
            SetQuietMode(false);
            Check("Eco reversed", !_preferences.EcoMode && _monitorTimer.Interval.TotalSeconds == 5);
            Navigate("home");
            V4NewTask.Text = AstraOS.Core.UiText.T("QA automated persistence"); V4AddTask_Click(this, new RoutedEventArgs());
            Check("Task storage DPAPI", _v4TaskStore.Load().Any(t => t.Text == "QA automated persistence"));
            var task = _v4Tasks.First(t => t.Text == "QA automated persistence");
            var completed = new Microsoft.UI.Xaml.Controls.CheckBox { Tag = task.Id, IsChecked = true };
            V4TaskChecked(completed, new RoutedEventArgs());
            Check("Task completion chart", V4WeekChart.Children.OfType<Microsoft.UI.Xaml.Controls.Border>().Any(b => b.Height > 0));
            await Task.Run(() => _notes.Add("QA aerospace", "Quaternion control and Kalman navigation", new[] { "mission" }));
            var notes = await Task.Run(_notes.Load);
            Check("Notes and tags persisted", notes.Any(n => n.Tags.Contains("mission")));
            Check("BM25 retrieval", NoteIndex.RetrievePassages(notes, "Kalman mission").Count > 0);
            _dataset = ScientificDataset.Parse("computed-orbit.csv", "radius_km,speed_km_s\n6778.137,7.668558\n7000,7.546053", false);
            Navigate("stem");
            Check("Mission Data Lab chart created", WorkspacePanel.Children.Count >= 5 && _dataset.Rows.Count == 2);
            var circular = OrbitalCalculator.Circular(6778.137, 398600.4418);
            Check("Engineering Studio formula", Math.Abs(circular.SpeedKmS - 7.668558) < 0.00001);
            await RenderPluginsAsync();
            Check("Declarative plugin discovered", PluginsList.Children.Count > 0);
            var recipe = _plugins.DiscoverDeclarative().FirstOrDefault();
            if (recipe.Plugin is not null) Check("Declarative plugin executes data operation", _plugins.ExecuteDeclarative(recipe.Plugin, recipe.Folder, string.IsNullOrEmpty(recipe.Plugin.Example)?"QA local data":recipe.Plugin.Example).Length > 0);
            Check("At least 30 bundled plugins", _plugins.DiscoverDeclarative().Count>=30);
            await RefreshLiveMetricsAsync(force: true);
            Check("System measurements", V4RamBar.Value > 0 && !string.IsNullOrWhiteSpace(V4DiskText.Text));
            var gifPath=Path.Combine(AstraPaths.UserDataRoot,"qa-motion.gif");
            await File.WriteAllBytesAsync(gifPath,Convert.FromBase64String("R0lGODlhAQABAIAAAEp/p7PP5SH/C05FVFNDQVBFMi4wAwEAAAAh+QQACgAAACwAAAAAAQABAAACAkQBACH5BAAKAAAALAAAAAABAAEAAAICTAEAOw=="));
            _preferences.BannerImagePath=gifPath;await LoadPersonalArtAsync();
            Check("Custom animated GIF decoded",BannerArtworkImage.Source is Microsoft.UI.Xaml.Media.Imaging.BitmapImage { IsAnimatedBitmap:true });
            SetQuietMode(true);
            Check("Eco stops animated image",BannerArtworkImage.Source is Microsoft.UI.Xaml.Media.Imaging.BitmapImage { IsPlaying:false });
            _preferences.BannerImagePath="";SetQuietMode(false);await LoadPersonalArtAsync();
            Check("Windows account shown", UserAccountName.Text.Contains(Environment.UserName,StringComparison.OrdinalIgnoreCase));
            _studyDomainBox!.SelectedValue="mathematics";
            Check("Study profile persisted", _preferenceStore.Load().StudyDomain=="mathematics" && !SimulationLab.For(_preferences.StudyDomain,_preferences.EngineeringTrack).Any(t=>t.Id=="flight"));
            _studyDomainBox.SelectedValue="engineering";_studyTrackBox!.SelectedValue="aerospace";
            Navigate("files");await LoadFilesAsync(AstraPaths.UserDataRoot);
            Check("Custom explorer reads real directory",_fileEntries.Count>0&&_filesDirectory==AstraPaths.UserDataRoot);
            var runtimeBefore=AiRuntimePath.Text;var modelBefore=AiModelPath.Text;AiRuntimePath.Text="";AiModelPath.Text="";
            var defaults=LocalAiPaths.Resolve(AppContext.BaseDirectory,AiRuntimePath.Text,AiModelPath.Text);
            Check("Empty AI paths resolve included resources",defaults.Runtime==Ai.BundledAi.RuntimePath&&defaults.Model==Ai.BundledAi.ModelPath);AiRuntimePath.Text=runtimeBefore;AiModelPath.Text=modelBefore;
            Check("Dock icons with tooltip",_dockButtons.Count==9&&VisualDescendants(HorizonDock).OfType<Microsoft.UI.Xaml.Controls.Button>().All(b=>b.Content is Microsoft.UI.Xaml.Controls.IconElement&&Microsoft.UI.Xaml.Controls.ToolTipService.GetToolTip(b) is string));
            Navigate("stem");_studioScene=SimulationLab.Run("orbit").Scene;await StartStudioAsync();
            var studioDeadline=DateTimeOffset.UtcNow.AddSeconds(15);
            while(!_studioReady&&_studioView is not null&&DateTimeOffset.UtcNow<studioDeadline)await Task.Delay(100);
            Check("Simulation 3D WebGL rendered",_studioReady,_studioState?.Text);
            var surface=ScientificScene.Parse(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory,"python-sdk","examples-data","mathematics.json")));
            _studioScene=surface with { Series=surface.Series.Concat(new[]{new SceneSeries("Campioni","points",new[]{new ScenePoint(0,0,0),new ScenePoint(1,1,1)})}).ToArray() };
            await StartStudioAsync();studioDeadline=DateTimeOffset.UtcNow.AddSeconds(10);
            while(!_studioReady&&_studioView is not null&&DateTimeOffset.UtcNow<studioDeadline)await Task.Delay(100);
            Check("Python surface and point cloud WebGL rendered",_studioReady,_studioState?.Text);
            Navigate("home");Check("Studio renderer released",_studioView is null);
            for (int cycle = 1; cycle <= 2; cycle++)
            {
                Navigate("orbit"); await Start3DAsync();
                var deadline = DateTimeOffset.UtcNow.AddSeconds(15);
                while (!_orbitReady && _orbitView is not null && DateTimeOffset.UtcNow < deadline) await Task.Delay(100);
                Check("WebGL ready cycle " + cycle, _orbitReady, OrbitStatus.Text);
                Check("Actual Desktop orbit cycle "+cycle,_desktopOrbitEntries.Count==FileExplorerService.ReadDesktop().Count&&_desktopOrbitEntries.Values.All(FileExplorerService.IsCurrentDesktopEntry));
                Navigate("home");
                Check("WebView2 released cycle " + cycle, _orbitView is null && OrbitViewHost.Children.Count == 0);
            }
            await RunExpansionSmokeAsync(Check);
            Navigate("home");
            StartupDiagnostics.Record("DesktopSmoke.complete");
        }
        catch (Exception ex)
        {
            checks.Add(new { name = "Unhandled QA failure", status = "FAIL", detail = ex.ToString() });
            StartupDiagnostics.Record("DesktopSmoke.failed", ex.ToString());
        }
        finally
        {
            var report = new { generated = DateTimeOffset.Now, checks };
            await File.WriteAllTextAsync(Path.Combine(AstraPaths.UserDataRoot, "desktop-smoke.json"),
                JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
