using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using AstraOS.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace AstraOS.Desktop;

/// <summary>Windows-first optional WSL 2 integration and low-overhead desktop shortcuts.</summary>
public sealed partial class MainWindow
{
    private void ApplyFocusMode(bool enabled)
    {
        SidebarShell.Visibility = enabled ? Visibility.Collapsed : Visibility.Visible;
        SidebarColumn.Width = enabled ? new GridLength(0) : new GridLength(218);
        FocusModeButton.Content = enabled ? AstraOS.Core.UiText.T("Mostra menu") : AstraOS.Core.UiText.T("Focus Mode");
        if (_preferences is not null) UpdateResponsiveLayout();
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(FocusModeButton,
            enabled ? AstraOS.Core.UiText.T("Mostra menu laterale") : AstraOS.Core.UiText.T("Attiva Focus Mode"));
    }

    private void FocusMode_Click(object sender, RoutedEventArgs e)
    {
        var previous = _preferences.FocusMode;
        _preferences.FocusMode = !previous;
        try
        {
            _preferenceStore.Save(_preferences);
            ApplyFocusMode(_preferences.FocusMode);
            StatusText.Text = _preferences.FocusMode ? AstraOS.Core.UiText.T("Focus Mode: usa dock, ricerca o Mostra menu") : AstraOS.Core.UiText.T("Menu laterale ripristinato");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _preferences.FocusMode = previous;
            ApplyFocusMode(previous);
            StatusText.Text = AstraOS.Core.UiText.T("Focus Mode non salvato: ") + ex.Message;
        }
    }

    private void DockVisibilityToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_settingToggles || _preferences is null || HorizonDock is null) return;
        bool previous = _preferences.ShowDock;
        bool show = DockVisibilityToggle.IsOn;
        _preferences.ShowDock = show;
        try
        {
            _preferenceStore.Save(_preferences);
            HorizonDock.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            StatusText.Text = show ? AstraOS.Core.UiText.T("Dock Horizon attivato") : AstraOS.Core.UiText.T("Dock nascosto: resta disponibile il menu laterale");
        }
        catch (IOException ex) { _preferences.ShowDock = previous; StatusText.Text = AstraOS.Core.UiText.T("Salvataggio non riuscito: ") + ex.Message; }
        catch (UnauthorizedAccessException ex) { _preferences.ShowDock = previous; StatusText.Text = AstraOS.Core.UiText.T("Accesso negato: ") + ex.Message; }
    }

    private void OpenDeviceEncryption_Click(object sender, RoutedEventArgs e) => OpenSystemSettings("ms-settings:deviceencryption");
    private void OpenCoreIsolation_Click(object sender, RoutedEventArgs e) => OpenSystemSettings("windowsdefender://coreisolation");

    private void UbuntuNav_Click(object sender, RoutedEventArgs e) => Navigate("ubuntu");

    private void HorizonSearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter) return;
        var destination = NavigationCatalog.Resolve(HorizonSearchBox.Text);
        if (destination is null)
            StatusText.Text = AstraOS.Core.UiText.T("Sezione non trovata. Prova: STEM, Ubuntu, AI, Gaming, Sicurezza, 3D, ISO.");
        else
        {
            Navigate(destination);
            HorizonSearchBox.Text = "";
            StatusText.Text = AstraOS.Core.UiText.T("Sezione aperta: ") + destination;
        }
        e.Handled = true;
    }

    private void HorizonSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (SearchSuggestions is null) return;
        SearchSuggestions.Visibility = string.IsNullOrWhiteSpace(HorizonSearchBox.Text) ? Visibility.Collapsed : Visibility.Visible;
        var suggestions = NavigationCatalog.Suggest(HorizonSearchBox.Text, 3);
        SearchSuggestions.Text = suggestions.Count == 0 ? AstraOS.Core.UiText.T("Ricerca locale · esempi: note, Blender, WSL, sicurezza") :
            AstraOS.Core.UiText.T("Vai a: ") + string.Join("  ·  ", suggestions.Select(x => x.Label));
    }

    private async void WslScan_Click(object sender, RoutedEventArgs e)
    {
        WslScanButton.IsEnabled = false;
        WslStatusText.Text = AstraOS.Core.UiText.T("Controllo WSL in corso…");
        try { WslStatusText.Text = await WslInspector.GetStatusAsync(); }
        catch (Exception ex) { WslStatusText.Text = AstraOS.Core.UiText.T("Controllo non riuscito: ") + ex.Message; }
        finally { WslScanButton.IsEnabled = true; }
    }

    private async void InstallUbuntu_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = AppRoot.XamlRoot,
            Title = AstraOS.Core.UiText.T("Installa Ubuntu tramite WSL 2?"),
            Content = new TextBlock { Text =
                AstraOS.Core.UiText.T("Questa azione richiama lo strumento ufficiale wsl.exe con privilegi amministrativi (UAC). ") +
                AstraOS.Core.UiText.T("Può scaricare pacchetti, abilitare componenti di virtualizzazione, occupare spazio sul disco e richiedere un riavvio. ") +
                AstraOS.Core.UiText.T("ASTRA non installa nulla automaticamente né disattiva la sicurezza di Windows."),
                TextWrapping = TextWrapping.Wrap },
            PrimaryButtonText = AstraOS.Core.UiText.T("Richiedi installazione"),
            CloseButtonText = AstraOS.Core.UiText.T("Annulla"),
            DefaultButton = ContentDialogButton.Close
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        if (!WslInspector.TryStartUbuntuInstall(out var message))
            WslStatusText.Text = message;
        else
            WslStatusText.Text = AstraOS.Core.UiText.T("UAC/installer Windows avviato; completare la procedura nella finestra esterna, poi ripetere la verifica.");
    }

    private async void LaunchUbuntu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Microsoft.UI.Xaml.Controls.Button button) button.IsEnabled = false;
        WslStatusText.Text = AstraOS.Core.UiText.T("Verifica della distribuzione Ubuntu…");
        try { WslStatusText.Text = await WslInspector.OpenUbuntuAsync(_windowLifetime.Token); }
        catch (OperationCanceledException) { if (!_windowClosed) WslStatusText.Text = AstraOS.Core.UiText.T("Controllo annullato."); }
        catch (Exception ex) { if (!_windowClosed) WslStatusText.Text = AstraOS.Core.UiText.T("Ubuntu non avviato: ") + ex.Message; }
        finally { if (!_windowClosed && sender is Microsoft.UI.Xaml.Controls.Button finished) finished.IsEnabled = true; }
    }

    private static void CopyLiteral(string command)
    {
        var payload = new DataPackage();
        payload.SetText(command);
        Clipboard.SetContent(payload);
    }
    private void CopyAptUpdate_Click(object sender, RoutedEventArgs e)
    {
        CopyLiteral("sudo apt update && sudo apt upgrade");
        WslStatusText.Text = AstraOS.Core.UiText.T("Comando copiato. Verifica le modifiche proposte da APT prima di confermare.");
    }
    private void CopyStemApt_Click(object sender, RoutedEventArgs e)
    {
        CopyLiteral("sudo apt update && sudo apt install python3 python3-venv python3-pip git build-essential cmake");
        WslStatusText.Text = AstraOS.Core.UiText.T("Comando copiato. Installazione non avviata: eseguila tu nel terminale Ubuntu.");
    }
    private void OpenWslGuide_Click(object sender, RoutedEventArgs e)
    {
        try { ChromeLauncher.Open("https://learn.microsoft.com/it-it/windows/wsl/install"); }
        catch (Exception ex) { WslStatusText.Text = AstraOS.Core.UiText.T("Chrome non disponibile: ") + ex.Message; }
    }
}

