using System;
using KumasKaliteKontrol.Data;
using KumasKaliteKontrol.Models;
using KumasKaliteKontrol.Services;
using System.Linq;
using KumasKaliteKontrol.Pdf;

internal class Program
{
    private static void Main(string[] args)
    {
        DbContext.CreateTables();

        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== KUMAS KALITE KONTROL SISTEMI ===\n");

            Console.Write("Kumas adi: ");
            string name = Console.ReadLine();

            Console.Write("Kumas kodu: ");
            string code = Console.ReadLine();

            Console.Write("Toplam uzunluk: ");
            int meters = int.Parse(Console.ReadLine());

            Fabric fabric = new Fabric
            {
                Name = name,
                Code = code,
                TotalMeters = meters
            };

            FabricRepository repo = new FabricRepository();
            int fabricId = repo.AddFabric(fabric);

            Console.WriteLine($"\nKumas kaydedildi. ID = {fabricId}");

            Console.WriteLine("\n--- HATA GIRISI ---");
            DefectRepository defectRepository = new DefectRepository();

            while (true)
            {
                Console.Write("Hata baslangici (-1 bitirir): ");
                int start = int.Parse(Console.ReadLine());
                if (start == -1) break;

                Console.Write("Hata bitisi: ");
                int end = int.Parse(Console.ReadLine());

                Console.Write("Hata turu (4=uzun, 1=aralik, 0=yok): ");
                int point = int.Parse(Console.ReadLine());

                int length = 0;

                if (point == 4 || point == 1)
                {
                    length = Math.Abs(end - start);
                    if (point == 1 && length == 0)
                        length = 1;
                }

                Defect defect = new Defect
                {
                    FabricId = fabricId,
                    StartMeter = start,
                    EndMeter = end,
                    PointType = point,
                    Length = length
                };

                defectRepository.AddDefect(defect);
                Console.WriteLine($"Hata eklendi (Uzunluk: {length})\n");
            }

            Console.WriteLine("Tum hatalar kaydedildi.");

            Console.WriteLine("\nPARTI HESAPLAMA BASLIYOR...\n");

            DefectReader reader = new DefectReader();
            var defects = reader.GetDefectsByFabric(fabricId);

            PartyCalculator calculator = new PartyCalculator();
            var parties = calculator.CreateParties(meters, defects, fabricId);


            int first = 0, second = 0;

            Console.WriteLine("--- PARTI SONUCLARI ---\n");

            foreach (var p in parties)
            {
                Console.WriteLine($"[{p.StartMeter}-{p.EndMeter}]  {p.Length}m  Puan:{p.TotalPoints}  Kalite:{p.Quality}");

                if (p.Quality == QualityLevel.FirstQuality)
                    first++;
                else
                    second++;
            }

            Console.WriteLine($"\n1. Kalite: {first}");
            Console.WriteLine($"2. Kalite: {second}");

            Console.WriteLine("\n--- UZUNLUK OZETI ---");

            var lengthSummary = parties
                .GroupBy(p => p.Length)
                .OrderByDescending(g => g.Key)
                .ToList();

            foreach (var group in lengthSummary)
                Console.WriteLine($"{group.Key} metre: {group.Count()} adet");

            Console.WriteLine("\nPDF rapor olusturulsun mu? (E/H): ");
            string pdfChoice = Console.ReadLine().ToLower();

            if (pdfChoice == "e")
            {
                PdfReportGenerator pdfReportGenerator = new PdfReportGenerator();
                pdfReportGenerator.GenerateReport(fabric, fabric.Id, parties);

            }

            Console.WriteLine("\nYeni bir kumas icin devam etmek ister misiniz? (E/H)");
            string again = Console.ReadLine().ToLower();

            if (again != "e")
                break;
        }

        Console.WriteLine("\nProgram sonlandi.");
        Console.ReadLine();
    }
}