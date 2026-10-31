using Microsoft.UI.Xaml;

namespace AstraOS.Desktop;

public partial class App : Application
{
    private Window? _mainWindow;

    public App()
    {
        // Diagnostic tracing only. Do not suppress exceptions that should reach
        // Visual Studio's debugger, including WinUI XAML/COM startup failures.
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            StartupDiagnostics.Record("AppDomain.UnhandledException", args.ExceptionObject?.ToString());
        UnhandledException += (_, args) =>
            StartupDiagnostics.Record("WinUI.UnhandledException", args.Exception?.ToString() ?? args.Message);
        AppDomain.CurrentDomain.FirstChanceException += (_, args) =>
        {
            if (args.Exception is Microsoft.UI.Xaml.Markup.XamlParseException ||
                args.Exception is System.Runtime.InteropServices.COMException)
                StartupDiagnostics.Record("WinUI.FirstChanceException", args.Exception.ToString());
        };

        try
        {
            StartupDiagnostics.Record("App.InitializeComponent.begin");
            InitializeComponent();
            StartupDiagnostics.Record("App.InitializeComponent.complete");
            // Do not access Application.Resources here: WinUI 3 can return
            // E_UNEXPECTED (0x8000FFFF) before the first Window is created.
            // Resource access, if needed, belongs after Window initialization.
        }
        catch (Exception ex)
        {
            StartupDiagnostics.Record("App.InitializeComponent.failed", ex.ToString());
            throw;
        }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            if (Environment.GetEnvironmentVariable("ASTRA_DIAGNOSTIC_XAML") is string diagnosticXaml &&
                !string.IsNullOrWhiteSpace(diagnosticXaml))
            {
                StartupDiagnostics.Record("XamlProbe.begin", diagnosticXaml);
                var probe = new Window();
                _mainWindow = probe;
                try
                {
                    probe.Content = (UIElement)Microsoft.UI.Xaml.Markup.XamlReader.Load(
                        System.IO.File.ReadAllText(diagnosticXaml));
                    StartupDiagnostics.Record("XamlProbe.complete");
                }
                catch (Exception ex)
                {
                    StartupDiagnostics.Record("XamlProbe.failed", ex.ToString());
                    probe.Content = new Microsoft.UI.Xaml.Controls.TextBlock
                    { Text = ex.ToString(), TextWrapping = TextWrapping.Wrap };
                }
                probe.Activate();
                return;
            }
            // Optional local diagnostic: use a minimal code-built WinUI window.
            // This separates a Windows App SDK/native-resource failure from
            // failure parsing ASTRA's 693-element MainWindow.xaml tree.
            if (Environment.GetEnvironmentVariable("ASTRA_DIAGNOSTIC_MINIMAL") == "1")
            {
                StartupDiagnostics.Record("MinimalWindow.create.begin");
                var minimalWindow = new Window();
                var root = new Microsoft.UI.Xaml.Controls.Grid();
                root.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock
                {
                    Text = AstraOS.Core.UiText.T("ASTRA OS — Test WinUI 3 minimale riuscito\n") +
                           AstraOS.Core.UiText.T("La finestra completa non e' stata caricata."),
                    FontSize = 23,
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                });
                minimalWindow.Content = root;
                _mainWindow = minimalWindow;
                StartupDiagnostics.Record("MinimalWindow.create.complete");
                minimalWindow.Activate();
                StartupDiagnostics.Record("MinimalWindow.activate.complete");
                return;
            }

            UiLocalization.Initialize();
            StartupDiagnostics.Record("MainWindow.create.begin");
            _mainWindow = new MainWindow();
            StartupDiagnostics.Record("MainWindow.create.complete");
            _mainWindow.Activate();
            StartupDiagnostics.Record("MainWindow.activate.complete");
        }
        catch (Exception ex)
        {
            StartupDiagnostics.Record("MainWindow.launch.failed", ex.ToString());
            throw;
        }
    }
}
