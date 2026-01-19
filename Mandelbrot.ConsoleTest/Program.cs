using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mandelbrot.Core;
using Mandelbrot.Core.Calculators;
using Mandelbrot.Core.Testing;


namespace Mandelbrot.ConsoleTest
{
    class Program
    {
        static void Main()
        {
            var factory = new CalculatorFactory();
            var runner = new RenderTestRunner();
            var palette = new ColorPalette();

            string outDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MandelbrotResults");
            Directory.CreateDirectory(outDir);

            var calc = new Mandelbrot.Core.Calculators.CpuParallelCalculator();

            // --- NOWA LOGIKA: Nazwa z datą i godziną ---
            // Pobieramy aktualny czas i formatujemy go: ddMMHHmm (dzień, miesiąc, godzina, minuta)
            string timestamp = DateTime.Now.ToString("ddMHHmm");
            string filePath = Path.Combine(outDir, $"Skalowalnosc_{timestamp}.csv");
            // -------------------------------------------

            Console.WriteLine("╔════════════════════════════════════════════╗");
            Console.WriteLine($"║    START TESTU (Zapis do: {Path.GetFileName(filePath)})");
            Console.WriteLine("╚════════════════════════════════════════════╝\n");

            for (int t = 1; t <= 64; t++)
            {
                calc.ThreadCount = t;

                Console.Write($"Testowanie: {t,2} wątków... ");

                var result = runner.Run(
                    calc,
                    width: 2000,
                    height: 2000,
                    viewPort: ViewPort.Default,
                    maxIterations: 1000,
                    palette: palette
                );

                result.CalculatorName = "Parallel";

                CsvResultWriter.Append(filePath, result);

                Console.WriteLine($"Czas: {result.RenderTimeMs,5} ms");
            }

            Console.WriteLine("\nTest zakończony. Plik zapisany pomyślnie.");
            calc.Dispose();
            Console.ReadKey();
        }
    }
    
}
