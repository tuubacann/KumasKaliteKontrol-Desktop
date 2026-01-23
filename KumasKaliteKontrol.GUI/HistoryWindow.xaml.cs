using System.Linq;
using System.Windows;
using System.Windows.Controls;
using KumasKaliteKontrol.Data;
using KumasKaliteKontrol.Models;

namespace KumasKaliteKontrol.GUI
{
    public partial class HistoryWindow : Window
    {
        private readonly FabricRepository _fabricRepo = new FabricRepository();
        private readonly PartyRepository _partyRepo = new PartyRepository();

        public HistoryWindow()
        {
            InitializeComponent();
            LoadFabrics();
        }

        private void LoadFabrics()
        {
            var fabrics = _fabricRepo.GetAllFabrics()
                .Select(f => new FabricItem
                {
                    Id = f.Id,
                    Display = $"{f.Id} - {f.Name} ({f.Code}) - {f.TotalMeters} m"
                })
                .ToList();

            LstFabrics.ItemsSource = fabrics;
        }

        private void LstFabrics_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstFabrics.SelectedItem is not FabricItem item)
                return;

            var parties = _partyRepo.GetPartiesByFabric(item.Id);

            var view = parties.Select(p =>
            {
                int defectLength = p.TotalPoints;
                double qualityRatio = p.Length == 0 ? 0 : (double)p.TotalPoints / p.Length;

                return new
                {
                    p.StartMeter,
                    p.EndMeter,
                    p.Length,
                    DefectLength = defectLength,
                    QualityRatio = $"{qualityRatio:P1}",
                    Quality = p.Quality == QualityLevel.FirstQuality
                                ? "1. Kalite"
                                : "2. Kalite",
                    TotalPoints = p.TotalPoints
                };
            }).ToList();

            GridParties.ItemsSource = view;

            int first = parties.Count(p => p.Quality == QualityLevel.FirstQuality);
            int second = parties.Count(p => p.Quality == QualityLevel.SecondQuality);

            var lengthLines = parties
                .GroupBy(p => p.Length)
                .OrderByDescending(g => g.Key)
                .Select(g => $"{g.Key} m → {g.Count()} parti");

            TxtSummary.Text =
                $"1. Kalite: {first} | 2. Kalite: {second}\n\n" +
                string.Join("\n", lengthLines);
        }

        private class FabricItem
        {
            public int Id { get; set; }
            public string Display { get; set; } = "";
        }
    }
}
