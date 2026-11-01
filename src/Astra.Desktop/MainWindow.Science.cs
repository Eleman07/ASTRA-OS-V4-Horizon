using System.Globalization;
using System.Diagnostics;
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
    private ScientificDataset? _dataset;
    private void AddScientificPanels()
    {
        AddStudioPanel();
        AddSimulationPanel();
        var engineering = new StackPanel { Spacing = 10 };
        engineering.Children.Add(Label(AstraOS.Core.UiText.T("ENGINEERING STUDIO / ORBITA CIRCOLARE"), 18));
        engineering.Children.Add(Label(AstraOS.Core.UiText.T("Modello a due corpi: v = √(μ/r), T = 2πr/v. Raggio dal centro del corpo, km; μ in km³/s². Nessuna perturbazione o simulazione di volo."), 12));
        var radius = new NumberBox { Header = AstraOS.Core.UiText.T("Raggio orbitale (km)"), Value = 6778.137, Minimum = 0.001, Maximum = 1e12, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact };
        var mu = new NumberBox { Header = AstraOS.Core.UiText.T("Parametro gravitazionale μ (km³/s²)"), Value = 398600.4418, Minimum = 0.000001, Maximum = 1e20 };
        var output = Label(AstraOS.Core.UiText.T("Valori iniziali: raggio terrestre equatoriale + 400 km; μ della Terra."));
        engineering.Children.Add(radius); engineering.Children.Add(mu);
        engineering.Children.Add(Action(AstraOS.Core.UiText.T("Calcola velocità e periodo"), (_, _) =>
        {
            try { var result = OrbitalCalculator.Circular(radius.Value, mu.Value); output.Text = AstraOS.Core.UiText.F("Velocità: {0:F6} km/s\nPeriodo: {1:F2} s ({2:F2} min)\nCalcolo deterministico a due corpi, orbita circolare.", result.SpeedKmS, result.PeriodSeconds, result.PeriodSeconds / 60); }
            catch (Exception ex) { output.Text = AstraOS.Core.UiText.T("Calcolo non valido: ") + ex.Message; }
        }));
        engineering.Children.Add(output);
        engineering.Children.Add(Action(AstraOS.Core.UiText.T("Apri simulatore esterno…"), async (_, _) =>
        {
            try
            {
                var picker = new FileOpenPicker(); picker.FileTypeFilter.Add(".exe");
                WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(this));
                var executable = await picker.PickSingleFileAsync(); if (executable is null || _windowClosed) return;
                var dialog = new ContentDialog { XamlRoot = AppRoot.XamlRoot, Title = AstraOS.Core.UiText.T("Avvia simulatore selezionato?"),
                    Content = executable.Path + AstraOS.Core.UiText.T("\nIl programma verrà eseguito con i privilegi del tuo account."),
                    PrimaryButtonText = AstraOS.Core.UiText.T("Avvia"), CloseButtonText = AstraOS.Core.UiText.T("Annulla"), DefaultButton = ContentDialogButton.Close };
                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                    Process.Start(new ProcessStartInfo(executable.Path) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(executable.Path) });
            }
            catch (Exception ex) { if (!_windowClosed) StatusText.Text = AstraOS.Core.UiText.T("Simulatore: ") + ex.Message; }
        }));
        engineering.Children.Add(Action(AstraOS.Core.UiText.T("Copia workflow Python / Jupyter"), (_, _) =>
        {
            CopyLiteral("py -m venv .venv\n.venv\\Scripts\\python -m pip install jupyterlab numpy scipy matplotlib\n.venv\\Scripts\\python -m jupyter lab");
            StatusText.Text = AstraOS.Core.UiText.T("Workflow copiato. Eseguilo nella cartella del progetto dopo aver verificato i pacchetti.");
        }));
        if (_preferences.StudyDomain is "general" or "physics" || _preferences.StudyDomain == "engineering" && _preferences.EngineeringTrack is "aerospace" or "other")
            WorkspacePanel.Children.Add(Card(new Expander { Header = AstraOS.Core.UiText.T("Calcolatore orbitale circolare e strumenti esterni"), Content = engineering, HorizontalAlignment = HorizontalAlignment.Stretch }));

        var lab = new StackPanel { Spacing = 10 };
        lab.Children.Add(Label(AstraOS.Core.UiText.T("MISSION DATA LAB"), 18));
        lab.Children.Add(Label(AstraOS.Core.UiText.T("Importa dati reali: CSV con intestazione, separatore virgola e decimali con punto; JSON come array di oggetti. Limiti: 8 MB, 10.000 righe, 64 colonne. Le celle non numeriche sono escluse dal grafico."), 12));
        var path = new TextBox { Header = AstraOS.Core.UiText.T("Dataset locale CSV / JSON"), PlaceholderText = "C:\\Dati\\mission.csv" };
        var state = Label(_dataset is null ? AstraOS.Core.UiText.T("Nessun dataset importato.") : AstraOS.Core.UiText.F("Dataset in memoria: {0}", _dataset.Name), 12);
        var columns = new ComboBox { Header = AstraOS.Core.UiText.T("Colonna Y · asse X = indice riga"), HorizontalAlignment = HorizontalAlignment.Stretch };
        var chart = new Polyline { Stroke = Token("AstraIce"), StrokeThickness = 2 };
        var chartCanvas = new Canvas { Width = 640, Height = 180, Background = Token("AstraNavy") };
        chartCanvas.Children.Add(chart);
        var chartView = new Viewbox { Child = chartCanvas, Stretch = Stretch.Uniform, MaxHeight = 220, HorizontalAlignment = HorizontalAlignment.Stretch };
        var statistics = Label(AstraOS.Core.UiText.T("Statistiche e grafico appariranno dopo l'importazione."), 12);
        var table = new TextBlock { FontFamily = new FontFamily("Cascadia Mono"), FontSize = 12, IsTextSelectionEnabled = true, TextWrapping = TextWrapping.Wrap, Foreground = Token("AstraDeepText") };
        void Draw()
        {
            if (_dataset is null || columns.SelectedIndex < 0) return;
            int column = columns.SelectedIndex;
            var values = _dataset.Rows.Select((r, i) => (Index:i, Value:r[column])).Where(v => v.Value.HasValue).ToArray();
            chart.Points = new PointCollection();
            if (values.Length == 0) { statistics.Text = AstraOS.Core.UiText.T("Colonna senza valori numerici finiti."); return; }
            double min = values.Min(v => v.Value!.Value), max = values.Max(v => v.Value!.Value);
            double scale = Math.Max(Math.Abs(min), Math.Abs(max)); if (scale == 0) scale = 1;
            double lo = min / scale, range = max / scale - lo;
            // Bound rendering independently from dataset size, preserving full statistics.
            int step = Math.Max(1, (int)Math.Ceiling(values.Length / 1000.0));
            foreach (var v in values.Where((_, i) => i % step == 0).Append(values[^1]).Distinct())
                chart.Points.Add(new Point(10 + 620.0 * v.Index / Math.Max(1, _dataset.Rows.Count - 1),
                    170 - (range > 0 ? (v.Value!.Value / scale - lo) / range : 0.5) * 160));
            double mean = values.Sum(v => v.Value!.Value / scale / values.Length) * scale;
            statistics.Text = AstraOS.Core.UiText.F("{0} · n={1} · min={2:G6} · max={3:G6} · media={4:G6}\nAsse Y: {5:G6} → {6:G6}; asse X: righe 0 → {7}. Grafico fino a 1.001 punti; statistiche su tutti i campioni.", _dataset.Columns[column], values.Length, min, max, mean, min, max, _dataset.Rows.Count - 1);
            table.Text = string.Join(" | ", _dataset.Columns) + "\n" + string.Join("\n", _dataset.Rows.Take(12).Select(r => string.Join(" | ", r.Select(v => v?.ToString("G6", CultureInfo.InvariantCulture) ?? "—"))));
        }
        void UpdateDataset()
        {
            columns.ItemsSource = _dataset?.Columns; columns.SelectedIndex = 0; Draw();
        }
        columns.SelectionChanged += (_, _) => Draw();
        var import = Action(AstraOS.Core.UiText.T("Importa dataset"), async (_, _) =>
        {
            try
            {
                var filePath = Path.GetFullPath(path.Text.Trim().Trim('"'));
                if (!new[] { ".csv", ".json" }.Contains(Path.GetExtension(filePath), StringComparer.OrdinalIgnoreCase)) throw new InvalidDataException(AstraOS.Core.UiText.T("Seleziona un file .csv o .json."));
                if (!File.Exists(filePath) || new FileInfo(filePath).Length > 8_000_000) throw new InvalidDataException(AstraOS.Core.UiText.T("File mancante o superiore a 8 MB."));
                state.Text = AstraOS.Core.UiText.T("Importazione in corso…");
                var data = await Task.Run(async () => ScientificDataset.Parse(Path.GetFileName(filePath),
                    await File.ReadAllTextAsync(filePath, _windowLifetime.Token), Path.GetExtension(filePath).Equals(".json", StringComparison.OrdinalIgnoreCase)), _windowLifetime.Token);
                if (_windowClosed) return;
                _dataset = data; state.Text = AstraOS.Core.UiText.F("{0}\n{1} righe · {2} colonne · dati importati, nessuna telemetria simulata", filePath, data.Rows.Count, data.Columns.Count);
                UpdateDataset();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (!_windowClosed) state.Text = AstraOS.Core.UiText.T("Importazione non riuscita: ") + ex.Message; }
        });
        lab.Children.Add(path);
        lab.Children.Add(Action(AstraOS.Core.UiText.T("Sfoglia dataset…"), async (_, _) =>
        {
            try
            {
                var picker = new FileOpenPicker(); picker.FileTypeFilter.Add(".csv"); picker.FileTypeFilter.Add(".json");
                WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(this));
                var file = await picker.PickSingleFileAsync(); if (file is not null && !_windowClosed) path.Text = file.Path;
            }
            catch (Exception ex) { state.Text = AstraOS.Core.UiText.T("Selezione file: ") + ex.Message; }
        }));
        lab.Children.Add(import); lab.Children.Add(state); lab.Children.Add(columns); lab.Children.Add(chartView);
        lab.Children.Add(statistics); lab.Children.Add(Label(AstraOS.Core.UiText.T("ANTEPRIMA · PRIME 12 RIGHE"), 12)); lab.Children.Add(table);
        WorkspacePanel.Children.Add(Card(lab)); if (_dataset is not null) UpdateDataset();
    }
}
