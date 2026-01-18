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

            string outDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "MandelbrotResults"
            );

            Directory.CreateDirectory(outDir);

            foreach (var name in factory.AvailableCalculators)
            {
                var calc = factory.GetCalculator(name);

                Console.WriteLine($"Testowanie: {name}...");

                var result = runner.Run(
                    calc,
                    width: 1000,
                    height: 1000,
                    viewPort: ViewPort.Default,
                    maxIterations: 500,
                    palette: palette
                );

                // NAPRAWA BŁĘDU ŚCIEŻKI: Usuwamy znaki specjalne z nazwy pliku
                string safeName = name.Replace("/", "_")
                                      .Replace("\\", "_")
                                      .Replace(":", "_")
                                      .Replace("(", "")
                                      .Replace(")", "");

                string file = Path.Combine(outDir, $"{safeName}.csv");

                CsvResultWriter.Append(file, result);
                Console.WriteLine($"✓ Wynik zapisany w: {safeName}.csv");
            }
        }
    }
}
