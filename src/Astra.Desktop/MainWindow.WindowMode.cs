using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace AstraOS.Desktop;

public sealed partial class MainWindow
{
    private ToggleSwitch? _startFullScreenToggle, _startAtLoginToggle;
    private TextBlock? _windowModeStatus;
    private AppWindowPresenter? _windowedPresenter;
    private bool _settingWindowMode;

    private void InitializeWindowMode()
    {
        var panel = new StackPanel { Spacing = 10 };
        panel.Children.Add(Label(AstraOS.Core.UiText.T("SCHERMO INTERO E AVVIO"), 19));
        panel.Children.Add(Label(AstraOS.Core.UiText.T("Apri ASTRA su tutto il monitor, coprendo anche la barra delle applicazioni Windows. F11 o il pulsante in alto a destra permettono di tornare alla finestra."), 12));
        _startFullScreenToggle = new ToggleSwitch { Header = AstraOS.Core.UiText.T("Avvia ASTRA a schermo intero"), IsOn = _preferences.StartFullScreen };
        _startFullScreenToggle.Toggled += StartFullScreenToggled;
        panel.Children.Add(_startFullScreenToggle);
        _startAtLoginToggle = new ToggleSwitch { Header = AstraOS.Core.UiText.T("Avvia ASTRA all'accesso a Windows") };
        _windowModeStatus = Label(AstraOS.Core.UiText.T("Le impostazioni si applicano al tuo account Windows."), 12);
        try { _startAtLoginToggle.IsOn = LoginStartup.IsCurrentCopyRegistered(); }
        catch (Exception ex) { _windowModeStatus.Text = AstraOS.Core.UiText.T("Avvio automatico non leggibile: ") + ex.Message; }
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASTRA_DATA_DIRECTORY")))
        {
            _startAtLoginToggle.IsEnabled = false;
            _windowModeStatus.Text = AstraOS.Core.UiText.T("Profilo QA: l'avvio automatico di Windows resta invariato.");
        }
        _startAtLoginToggle.Toggled += StartAtLoginToggled;
        panel.Children.Add(_startAtLoginToggle);
        panel.Children.Add(Label(AstraOS.Core.UiText.T("L'avvio automatico avviene dopo l'accesso al tuo account. Mantieni ASTRA nella stessa cartella; se lo sposti, disattiva e riattiva questa opzione dalla nuova copia."), 12));
        panel.Children.Add(Action(AstraOS.Core.UiText.T("App di avvio nelle impostazioni Windows"), (_, _) => OpenSystemSettings("ms-settings:startupapps")));
        panel.Children.Add(_windowModeStatus);
        ThemePanel.Children.Insert(0, Card(panel));

        var f11 = new KeyboardAccelerator { Key = VirtualKey.F11 };
        f11.Invoked += (_, e) => { ToggleFullScreen(); e.Handled = true; };
        AppRoot.KeyboardAccelerators.Add(f11);
        var escape = new KeyboardAccelerator { Key = VirtualKey.Escape };
        escape.Invoked += (_, e) =>
        {
            if (AppWindow.Presenter.Kind != AppWindowPresenterKind.FullScreen) return;
            ApplyFullScreen(false); e.Handled = true;
        };
        AppRoot.KeyboardAccelerators.Add(escape);

        var args = Environment.GetCommandLineArgs();
        bool fullScreen = args.Contains("--fullscreen", StringComparer.OrdinalIgnoreCase) ||
            (_preferences.StartFullScreen && !args.Contains("--windowed", StringComparer.OrdinalIgnoreCase));
        ApplyFullScreen(fullScreen);
    }

    private void ApplyFullScreen(bool enabled)
    {
        try
        {
            if (enabled && AppWindow.Presenter.Kind != AppWindowPresenterKind.FullScreen)
            {
                _windowedPresenter = AppWindow.Presenter;
                AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
            }
            else if (!enabled && AppWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen)
                AppWindow.SetPresenter(_windowedPresenter ?? OverlappedPresenter.Create());
            bool active = AppWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen;
            var name = active ? "Torna alla finestra (F11)" : "Schermo intero (F11)";
            FullScreenButton.Content = active ? AstraOS.Core.UiText.T("Esci da schermo intero") : AstraOS.Core.UiText.T("Schermo intero");
            ToolTipService.SetToolTip(FullScreenButton, name);
            AutomationProperties.SetName(FullScreenButton, name);
        }
        catch (Exception ex)
        {
            StatusText.Text = AstraOS.Core.UiText.T("Modalità finestra: ") + ex.Message;
            StartupDiagnostics.Record("WindowMode.failed", ex.ToString());
        }
    }

    private void ToggleFullScreen() => ApplyFullScreen(AppWindow.Presenter.Kind != AppWindowPresenterKind.FullScreen);
    private void FullScreen_Click(object sender, RoutedEventArgs e) => ToggleFullScreen();

    private void StartFullScreenToggled(object sender, RoutedEventArgs e)
    {
        if (_settingWindowMode || _startFullScreenToggle is null) return;
        bool previous = _preferences.StartFullScreen;
        try
        {
            _preferences.StartFullScreen = _startFullScreenToggle.IsOn;
            _preferenceStore.Save(_preferences);
            ApplyFullScreen(_preferences.StartFullScreen);
            _windowModeStatus!.Text = _preferences.StartFullScreen
                ? AstraOS.Core.UiText.T("Salvato: ASTRA si aprirà a schermo intero. F11 o Esc escono dalla modalità attuale.")
                : AstraOS.Core.UiText.T("Salvato: ASTRA si aprirà in una finestra.");
        }
        catch (Exception ex)
        {
            _preferences.StartFullScreen = previous;
            _settingWindowMode = true; _startFullScreenToggle.IsOn = previous; _settingWindowMode = false;
            _windowModeStatus!.Text = AstraOS.Core.UiText.T("Impostazione non salvata: ") + ex.Message;
        }
    }

    private void StartAtLoginToggled(object sender, RoutedEventArgs e)
    {
        if (_settingWindowMode || _startAtLoginToggle is null) return;
        bool enabled = _startAtLoginToggle.IsOn;
        try
        {
            LoginStartup.SetEnabled(enabled);
            if (LoginStartup.IsCurrentCopyRegistered() != enabled) throw new IOException(AstraOS.Core.UiText.T("Registrazione non confermata da Windows."));
            _windowModeStatus!.Text = enabled
                ? AstraOS.Core.UiText.T("ASTRA è registrato per l'accesso a Windows. In App di avvio verifica che Windows lo lasci abilitato.")
                : AstraOS.Core.UiText.T("Avvio automatico di ASTRA rimosso per il tuo account.");
        }
        catch (Exception ex)
        {
            _settingWindowMode = true; _startAtLoginToggle.IsOn = !enabled; _settingWindowMode = false;
            _windowModeStatus!.Text = AstraOS.Core.UiText.T("Avvio automatico non modificato: ") + ex.Message;
        }
    }
}
