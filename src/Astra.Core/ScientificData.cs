using System.Globalization;
using System.Text.Json;
using Microsoft.VisualBasic.FileIO;

namespace AstraOS.Core;

public sealed record ScientificDataset(string Name, IReadOnlyList<string> Columns, IReadOnlyList<double?[]> Rows)
{
    public const int MaxRows = 10_000;
    public static ScientificDataset Parse(string name, string text, bool json)
    {
        if (text.Length > 8_000_000) throw new InvalidDataException("Dataset: limite 8 MB.");
        var rows = new List<double?[]>();
        string[] columns;
        if (json)
        {
            using var doc = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 8 });
            if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
                throw new InvalidDataException("JSON: atteso un array di oggetti con colonne numeriche.");
            if (doc.RootElement.GetArrayLength() > MaxRows) throw new InvalidDataException("Limite 10.000 righe.");
            var first = doc.RootElement[0];
            if (first.ValueKind != JsonValueKind.Object) throw new InvalidDataException("JSON: righe non valide.");
            columns = first.EnumerateObject().Select(p => p.Name).ToArray();
            ValidateColumns(columns);
            foreach (var row in doc.RootElement.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Object) throw new InvalidDataException("JSON: righe non omogenee.");
                if (row.EnumerateObject().Any(p => !columns.Contains(p.Name, StringComparer.Ordinal)) ||
                    row.EnumerateObject().Select(p => p.Name).Distinct().Count() != row.EnumerateObject().Count())
                    throw new InvalidDataException("JSON: colonne sconosciute o duplicate.");
                rows.Add(columns.Select(c => row.TryGetProperty(c, out var v) ? Numeric(v.ToString()) : null).ToArray());
            }
        }
        else
        {
            using var parser = new TextFieldParser(new StringReader(text));
            parser.TextFieldType = FieldType.Delimited;
            parser.SetDelimiters(","); parser.HasFieldsEnclosedInQuotes = true;
            columns = parser.ReadFields() ?? throw new InvalidDataException("CSV vuoto.");
            ValidateColumns(columns);
            while (!parser.EndOfData)
            {
                if (rows.Count >= MaxRows) throw new InvalidDataException("Limite 10.000 righe.");
                var fields = parser.ReadFields()!;
                if (fields.Length != columns.Length) throw new InvalidDataException("CSV: numero di colonne incoerente.");
                rows.Add(fields.Select(Numeric).ToArray());
            }
        }
        if (rows.Count == 0 || !rows.Any(r => r.Any(v => v.HasValue)))
            throw new InvalidDataException("Nessun campione numerico finito. CSV: intestazione e separatore virgola, decimali con punto.");
        return new(Path.GetFileName(name), columns, rows);
    }
    private static double? Numeric(string value) => double.TryParse(value, NumberStyles.Float,
        CultureInfo.InvariantCulture, out var n) && double.IsFinite(n) ? n : null;
    private static void ValidateColumns(string[] columns)
    {
        if (columns.Length is < 1 or > 64 || columns.Any(c => string.IsNullOrWhiteSpace(c) || c.Length > 120 || c.Any(char.IsControl)) ||
            columns.Distinct(StringComparer.Ordinal).Count() != columns.Length)
            throw new InvalidDataException("Da 1 a 64 intestazioni univoche, massimo 120 caratteri.");
    }
}

public static class OrbitalCalculator
{
    /// <summary>Circular two-body orbit; radius in km, mu in km^3/s^2. Not a flight simulator.</summary>
    public static (double SpeedKmS, double PeriodSeconds) Circular(double radiusKm, double muKm3S2)
    {
        if (!double.IsFinite(radiusKm) || !double.IsFinite(muKm3S2) || radiusKm <= 0 || muKm3S2 <= 0)
            throw new ArgumentOutOfRangeException(nameof(radiusKm), "Raggio e parametro gravitazionale devono essere positivi e finiti.");
        var speed = Math.Sqrt(muKm3S2 / radiusKm);
        var period = 2 * Math.PI * radiusKm / speed;
        if (!double.IsFinite(speed) || !double.IsFinite(period)) throw new ArgumentOutOfRangeException(nameof(radiusKm));
        return (speed, period);
    }
}
