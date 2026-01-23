using System;
using System.Collections.Generic;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using KumasKaliteKontrol.Models;
using KumasKaliteKontrol.Data;

namespace KumasKaliteKontrol.Pdf
{
    public class PdfReportGenerator
    {
        public void GenerateReport(Fabric fabric, int fabricId, List<Party> parties)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var defects = new DefectReader().GetDefectsByFabric(fabricId);

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string reportFolder = System.IO.Path.Combine(desktopPath, "Raporlar");

            if (!System.IO.Directory.Exists(reportFolder))
                System.IO.Directory.CreateDirectory(reportFolder);

            string fileName = $"KumasRaporu_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.pdf";
            string filePath = System.IO.Path.Combine(reportFolder, fileName);

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    page.Header()
                        .Text("KUMAŞ KALİTE RAPORU")
                        .FontSize(20)
                        .Bold()
                        .AlignCenter();

                    page.Content().Column(col =>
                    {
                        col.Spacing(10);

                        col.Item().Text($"Kumaş Adı: {fabric.Name}");
                        col.Item().Text($"Kumaş Kodu: {fabric.Code}");
                        col.Item().Text($"Toplam Uzunluk: {fabric.TotalMeters} m");
                        col.Item().Text($"Tarih: {DateTime.Now:dd.MM.yyyy HH:mm}");

                        col.Item().LineHorizontal(1);

                        int first = parties.Count(p => p.Quality == QualityLevel.FirstQuality);
                        int second = parties.Count(p => p.Quality == QualityLevel.SecondQuality);

                        col.Item().Text($"1. Kalite Parti: {first}");
                        col.Item().Text($"2. Kalite Parti: {second}");

                        col.Item().LineHorizontal(1);

                        col.Item().Text("PARTİ DETAYLARI").Bold();

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(60);   
                                columns.ConstantColumn(60);   
                                columns.ConstantColumn(60);   
                                columns.ConstantColumn(80);   
                                columns.ConstantColumn(80);   
                                columns.ConstantColumn(70);   
                                columns.ConstantColumn(80);   
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Baş").Bold();
                                header.Cell().Text("Bitiş").Bold();
                                header.Cell().Text("Uz.").Bold();
                                header.Cell().Text("Hata Uz.").Bold();
                                header.Cell().Text("Oran").Bold();
                                header.Cell().Text("Kalite").Bold();
                                header.Cell().Text("Puan").Bold();
                            });

                            foreach (var p in parties)
                            {
                                int defectLength = defects
                                    .Where(d => d.StartMeter < p.EndMeter && d.EndMeter > p.StartMeter)
                                    .Sum(d =>
                                        Math.Min(d.EndMeter, p.EndMeter)
                                      - Math.Max(d.StartMeter, p.StartMeter));

                                double ratio =
                                    p.Length == 0 ? 0 : (double)defectLength / p.Length;

                                table.Cell().Text(p.StartMeter.ToString());
                                table.Cell().Text(p.EndMeter.ToString());
                                table.Cell().Text(p.Length.ToString());
                                table.Cell().Text(defectLength.ToString());
                                table.Cell().Text($"{ratio:P1}");
                                table.Cell().Text(
                                    p.Quality == QualityLevel.FirstQuality
                                        ? "1. Kalite"
                                        : "2. Kalite");
                                table.Cell().Text(p.TotalPoints.ToString());
                            }
                        });

                        col.Item().LineHorizontal(1);

                        col.Item().Text("UZUNLUK ÖZETİ").Bold();

                        var summary = parties
                            .GroupBy(p => p.Length)
                            .OrderByDescending(g => g.Key);

                        foreach (var g in summary)
                        {
                            col.Item().Text($"{g.Key} metre: {g.Count()} adet");
                        }
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text("Kumaş Kalite Kontrol Sistemi");
                });
            })
            .GeneratePdf(filePath);

            Console.WriteLine($"PDF oluşturuldu: {filePath}");
        }
    }
}
