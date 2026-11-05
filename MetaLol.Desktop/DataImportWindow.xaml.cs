using Microsoft.Win32;
using System.Windows;

namespace MetaLol.Desktop;

public partial class DataImportWindow : Window
{
    private readonly DatabaseService _database;
    private readonly RiotMatchImporter _importer;

    public DataImportWindow(DatabaseService database, string region, string tier)
    {
        InitializeComponent();
        _database = database;
        _importer = new(database);
        RouteBox.ItemsSource = new[] { "americas", "europe", "asia", "sea" };
        RegionInput.ItemsSource = new[] { "LAS", "LAN", "NA", "EUW", "KR", "GLOBAL" };
        TierInput.ItemsSource = new[] { "ALL", "GOLD_PLUS", "PLATINUM_PLUS", "EMERALD_PLUS", "DIAMOND_PLUS" };
        RegionInput.SelectedItem = region;
        TierInput.SelectedItem = tier;
        SelectRouteForRegion(region);
    }

    private async void ImportMatches_Click(object sender, RoutedEventArgs e)
    {
        var ids = MatchIdsBox.Text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (string.IsNullOrWhiteSpace(ApiKeyBox.Password) || ids.Length == 0)
        {
            StatusText.Text = "Ingresa una API key y al menos un Match ID.";
            return;
        }

        ImportButton.IsEnabled = false;
        try
        {
            var progress = new Progress<string>(message => StatusText.Text = message);
            var result = await _importer.ImportMatchIdsAsync(ids, ApiKeyBox.Password,
                RouteBox.SelectedItem as string ?? "americas", RegionInput.SelectedItem as string ?? "LAS",
                TierInput.SelectedItem as string ?? "ALL", progress);
            StatusText.Text = $"Listo: {result.Imported} importadas, {result.Skipped} omitidas.";
            DialogResult = result.Imported > 0;
        }
        catch (Exception ex) { StatusText.Text = $"Error: {ex.Message}"; }
        finally
        {
            ApiKeyBox.Clear();
            ImportButton.IsEnabled = true;
        }
    }

    private void RegionInput_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (RegionInput.SelectedItem is string region) SelectRouteForRegion(region);
    }

    private void SelectRouteForRegion(string region) =>
        RouteBox.SelectedItem = region is "EUW" ? "europe" : region is "KR" ? "asia" : "americas";

    private async void ImportCsv_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Counter CSV (*.csv)|*.csv", Title = "Importar counters" };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var count = await _database.ImportCountersCsvAsync(dialog.FileName);
            StatusText.Text = $"{count} filas importadas desde CSV.";
            if (count > 0) DialogResult = true;
        }
        catch (Exception ex) { StatusText.Text = $"Error: {ex.Message}"; }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
