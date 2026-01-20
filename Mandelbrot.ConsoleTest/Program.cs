using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mandelbrot.Core;
using Mandelbrot.Core.Calculators;
using Mandelbrot.ConsoleTest.Testing;


namespace Mandelbrot.ConsoleTest
{
    class Program
    {
        static void Main()
        {
            CalculatorFactory factory = new CalculatorFactory();
            RenderTestRunner runner = new RenderTestRunner();
            ColorPalette palette = new ColorPalette();

            string outDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MandelbrotResults");
            Directory.CreateDirectory(outDir);

            var calc = (CpuParallelCalculator)factory.GetCalculator("CPU Parallel (8 wątków)");

            string timestamp = DateTime.Now.ToString("ddMHHmm");
            string filePath = Path.Combine(outDir, $"Skalowalnosc_{timestamp}.csv");

            Console.WriteLine("╔════════════════════════════════════════════╗");
            Console.WriteLine($"║    START TESTU (Zapis do: {Path.GetFileName(filePath)})");
            Console.WriteLine("╚════════════════════════════════════════════╝\n");

            for (int t = 1; t <= 64; t++)
            {
                calc.ThreadCount = t;

                Console.Write($"Testowanie: {t,2} wątków... ");

                RenderTestResult result = runner.Run(
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
