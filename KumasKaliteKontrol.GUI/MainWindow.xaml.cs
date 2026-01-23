using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using KumasKaliteKontrol.Data;
using KumasKaliteKontrol.Models;
using KumasKaliteKontrol.Services;
using KumasKaliteKontrol.Pdf;

namespace KumasKaliteKontrol.GUI
{
    public partial class MainWindow : Window
    {
        private int _currentFabricId;
        private Fabric? _currentFabric;
        private List<Party> _currentParties = new();

        public MainWindow()
        {
            InitializeComponent();
            DbContext.CreateTables();
            LoadProducts();
        }

        private void LoadProducts()
        {
            CmbProduct.ItemsSource = ProductCatalog.Products;
        }

        private async Task ShowToastAsync(string message, Brush color)
        {
            TxtToast.Text = message;
            TxtToast.Foreground = color;
            ToastBorder.Visibility = Visibility.Visible;
            await Task.Delay(1600);
            ToastBorder.Visibility = Visibility.Collapsed;
        }

        private bool TryGetInt(TextBox box, out int value)
        {
            value = 0;
            return int.TryParse(box.Text.Trim(), out value);
        }

        private void BtnSaveFabric_Click(object sender, RoutedEventArgs e)
        {
            if (CmbProduct.SelectedItem is not Product product)
            {
                _ = ShowToastAsync("Ürün seçiniz!", Brushes.Orange);
                return;
            }

            if (!TryGetInt(TxtMeters, out int meters))
            {
                _ = ShowToastAsync("Toplam metre geçersiz!", Brushes.Orange);
                return;
            }

            var fabric = new Fabric
            {
                Name = product.Name,
                Code = product.Code,
                TotalMeters = meters
            };

            var repo = new FabricRepository();
            _currentFabricId = repo.AddFabric(fabric);
            _currentFabric = fabric;

            DefectGrid.ItemsSource = null;
            PartyGrid.ItemsSource = null;
            TxtQualitySummary.Text = "";
            TxtLengthSummary.Text = "";

            TxtMeters.Text = "";
            CmbProduct.SelectedIndex = -1;

            _ = ShowToastAsync("Kumaş kaydedildi!", Brushes.LightGreen);
        }

        private void BtnAddDefect_Click(object sender, RoutedEventArgs e)
        {
            if (_currentFabricId == 0)
            {
                _ = ShowToastAsync("Önce kumaş kaydet!", Brushes.Orange);
                return;
            }

            if (!TryGetInt(TxtStart, out int start))
            {
                _ = ShowToastAsync("Metre değerleri hatalı!", Brushes.Orange);
                return;
            }

            bool isLong =
                CmbDefectKind.SelectedItem is ComboBoxItem item &&
                item.Tag?.ToString() == "Long";

            int end = start;
            if (isLong && !TryGetInt(TxtEnd, out end))
            {
                _ = ShowToastAsync("Metre değerleri hatalı!", Brushes.Orange);
                return;
            }

            int realStart = Math.Min(start, end);
            int realEnd = Math.Max(start, end);

            var defect = new Defect
            {
                FabricId = _currentFabricId,
                StartMeter = realStart,
                EndMeter = realEnd,
                PointType = isLong ? 4 : 1,
                Length = isLong ? (realEnd - realStart) : 1
            };

            new DefectRepository().AddDefect(defect);

            DefectGrid.ItemsSource =
                new DefectReader().GetDefectsByFabric(_currentFabricId);

            TxtStart.Text = "";
            TxtEnd.Text = "";

            _ = ShowToastAsync("Hata eklendi!", Brushes.LightGreen);
        }

        private void BtnCalculate_Click(object sender, RoutedEventArgs e)
        {
            if (_currentFabric == null)
            {
                _ = ShowToastAsync("Önce kumaş kaydet!", Brushes.Orange);
                return;
            }

            var defects = new DefectReader().GetDefectsByFabric(_currentFabricId);
            var calculator = new PartyCalculator();

            _currentParties = calculator.CreateParties(
                _currentFabric.TotalMeters, defects, _currentFabricId);

            var repo = new PartyRepository();
            repo.DeletePartiesByFabric(_currentFabricId);

            foreach (var p in _currentParties)
                repo.AddParty(p);

            PartyGrid.ItemsSource = _currentParties.Select(p =>
            {
                int defectLen = defects
                    .Where(d => d.StartMeter < p.EndMeter && d.EndMeter > p.StartMeter)
                    .Sum(d =>
                        Math.Min(d.EndMeter, p.EndMeter)
                      - Math.Max(d.StartMeter, p.StartMeter));

                double ratio = p.Length == 0 ? 0 : (double)defectLen / p.Length;

                return new
                {
                    p.StartMeter,
                    p.EndMeter,
                    p.Length,
                    DefectLength = defectLen,
                    QualityRatio = $"{ratio:P1}",
                    Quality = p.Quality == QualityLevel.FirstQuality
                                ? "1. Kalite"
                                : "2. Kalite",
                    TotalPoints = p.TotalPoints
                };
            }).ToList();

            int firstCount = _currentParties.Count(p => p.Quality == QualityLevel.FirstQuality);
            int secondCount = _currentParties.Count(p => p.Quality == QualityLevel.SecondQuality);

            int totalPartyLength = _currentParties.Sum(p => p.Length);

            var grouped = _currentParties
                .GroupBy(p => p.Length)
                .OrderBy(g => g.Key)
                .Select(g => $"{g.Key} m : {g.Count()} parti");

            TxtQualitySummary.Text =
                $"1. Kalite: {firstCount} adet | 2. Kalite: {secondCount} adet";

            TxtLengthSummary.Text =
                $"Toplam Parti Uzunluğu: {totalPartyLength} metre\n" +
                string.Join("\n", grouped);

            int totalDefectLength = _currentParties.Sum(p =>
                defects
                    .Where(d => d.StartMeter < p.EndMeter && d.EndMeter > p.StartMeter)
                    .Sum(d =>
                        Math.Min(d.EndMeter, p.EndMeter)
                      - Math.Max(d.StartMeter, p.StartMeter))
            );

            double defectPercent =
                _currentFabric.TotalMeters == 0
                    ? 0
                    : (double)totalDefectLength / _currentFabric.TotalMeters;

            int firstQualityLength = _currentParties
                .Where(p => p.Quality == QualityLevel.FirstQuality)
                .Sum(p => p.Length);

            int secondQualityLength = _currentParties
                .Where(p => p.Quality == QualityLevel.SecondQuality)
                .Sum(p => p.Length);

            double firstQualityPercent =
                _currentFabric.TotalMeters == 0
                    ? 0
                    : (double)firstQualityLength / _currentFabric.TotalMeters;

            double secondQualityPercent =
                _currentFabric.TotalMeters == 0
                    ? 0
                    : (double)secondQualityLength / _currentFabric.TotalMeters;

            TxtQualitySummary.Text +=
                $"\n1. Kalite: {firstQualityLength} m ({firstQualityPercent:P1})" +
                $" | 2. Kalite: {secondQualityLength} m ({secondQualityPercent:P1})";

            TxtLengthSummary.Text +=
                $"\n\nToplam Kumaş: {_currentFabric.TotalMeters} m" +
                $"\nToplam Hata Uzunluğu: {totalDefectLength} m ({defectPercent:P1})";

        }


        private void BtnCreatePdf_Click(object sender, RoutedEventArgs e)
        {
            if (_currentFabric == null || !_currentParties.Any())
            {
                _ = ShowToastAsync("Önce parti hesapla!", Brushes.Orange);
                return;
            }

            new PdfReportGenerator()
                .GenerateReport(_currentFabric, _currentFabricId, _currentParties);

            _ = ShowToastAsync("PDF oluşturuldu!", Brushes.LightGreen);
        }

        private void BtnCreateExcel_Click(object sender, RoutedEventArgs e)
        {
            if (_currentFabric == null || !_currentParties.Any())
            {
                _ = ShowToastAsync("Önce parti hesapla!", Brushes.Orange);
                return;
            }

            try
            {
                var defects = new DefectReader().GetDefectsByFabric(_currentFabricId);

                string folder = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Raporlar");

                if (!System.IO.Directory.Exists(folder))
                    System.IO.Directory.CreateDirectory(folder);

                string path = System.IO.Path.Combine(
                    folder, $"KumasRaporu_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv");

                using var writer = new System.IO.StreamWriter(path, false, System.Text.Encoding.UTF8);

                writer.WriteLine("Başlangıç;Bitiş;Uzunluk;Hata Uzunluğu;Kalite Oranı;Kalite Türü;Kalite Hata Puanı");

                foreach (var p in _currentParties)
                {
                    int defectLen = defects
                        .Where(d => d.StartMeter < p.EndMeter && d.EndMeter > p.StartMeter)
                        .Sum(d => Math.Min(d.EndMeter, p.EndMeter) - Math.Max(d.StartMeter, p.StartMeter));

                    double ratio = (double)defectLen / p.Length;

                    writer.WriteLine(
                        $"{p.StartMeter};{p.EndMeter};{p.Length};{defectLen};{ratio:P1};" +
                        $"{(p.Quality == QualityLevel.FirstQuality ? "1. Kalite" : "2. Kalite")};{p.TotalPoints}");
                }

                _ = ShowToastAsync("Excel oluşturuldu!", Brushes.LightGreen);
            }
            catch
            {
                _ = ShowToastAsync("Excel oluşturulamadı!", Brushes.Red);
            }
        }

        private void BtnOpenHistory_Click(object sender, RoutedEventArgs e)
        {
            new HistoryWindow().Show();
        }
    }
}
