using Mandelbrot.ConsoleTest.Testing;
using Mandelbrot.Core.Calculators;
using Mandelbrot.Core.Rendering;
using System.Text;

namespace Mandelbrot.ConsoleTest;

internal class Program
{
    private static void Main()
    {
        using CalculatorFactory factory = new();
        RenderTestRunner runner = new();

        const int width = 2000;
        const int height = 2000;
        const int iterations = 1000;
        const int maxThreads = 32; 

        string outDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../MandelbrotTestResults"));
        if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

        SystemInfo.Print();

        string timestamp = DateTime.Now.ToString("ddMM_HHmm");
        string filePath = Path.Combine(outDir, $"Porownanie_Silnikow_{timestamp}.csv");

        var availableNames = factory.AvailableCalculators;

        using (StreamWriter writer = new(filePath, false, Encoding.UTF8))
        {
            writer.WriteLine($"# Test porównawczy silników - {DateTime.Now}");
            writer.WriteLine($"# Sprzęt: {SystemInfo.CpuName} | {SystemInfo.GpuName}");
            writer.WriteLine("#");

            string dynamicHeaders = string.Join(",", availableNames.Select(n => $"{n}_ms"));
            writer.WriteLine($"Threads,Width,Height,Iterations,{dynamicHeaders}");

            Console.WriteLine($"\nRozpoczynanie testu porównawczego...");
            Console.WriteLine($"Zapis do: {filePath}\n");

            for (int t = 1; t <= maxThreads; t++)
            {
                Console.Write($"Testowanie dla {t,2} wątków: ");
                List<long> resultsPerRow = new();

                foreach (string name in availableNames)
                {
                    var calc = factory.GetCalculator(name);

                    if (calc is CpuParallelCalculator parallelCalc)
                    {
                        parallelCalc.ThreadCount = t;
                    }

                    var result = runner.Run(calc, width, height, ViewPort.Default, iterations, t);
                    resultsPerRow.Add(result.RenderTimeMs);

                    Console.Write($"[{name}: {result.RenderTimeMs}ms] ");
                }

                string row = $"{t},{width},{height},{iterations}," + string.Join(",", resultsPerRow);
                writer.WriteLine(row);
                Console.WriteLine(" - OK");
            }
        }

        Console.WriteLine("\nTest zakończony sukcesem!");
        Console.WriteLine("Plik CSV jest gotowy do importu np. do Excela w celu zrobienia wykresu.");
        Console.ReadKey();
    }
}