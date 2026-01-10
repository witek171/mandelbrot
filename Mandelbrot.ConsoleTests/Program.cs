using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mandelbrot.Core;
using Mandelbrot.Core.Calculators;
using Mandelbrot.Core.Testing;

namespace Mandelbrot.ConsoleTests
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

            foreach (var calc in factory.GetAvailableCalculators())
            {
                var result = runner.Run(
                    calc,
                    width: 1000,
                    height: 1000,
                    viewPort: ViewPort.Default,
                    maxIterations: 500,
                    palette: palette
                );

                string file = Path.Combine(outDir, $"{calc.Name}.csv");
                CsvResultWriter.Append(file, result);

                calc.Dispose();
            }
        }
    }
}
