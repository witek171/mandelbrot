using Mandelbrot.ConsoleTest.Testing;
using Mandelbrot.Core.Calculators;
using Mandelbrot.Core.Rendering;

namespace Mandelbrot.ConsoleTest;

internal class Program
{
	private static void Main()
	{
		using CalculatorFactory factory = new();
		RenderTestRunner runner = new();

		string outDir = Path.GetFullPath(
			Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../MandelbrotTestResults"));

		if (!Directory.Exists(outDir))
			Directory.CreateDirectory(outDir);

		SystemInfo systemInfo = new();
		systemInfo.Print();

		CpuParallelCalculator calc = (CpuParallelCalculator)factory.GetCalculator("CPU Parallel");

		string timestamp = DateTime.Now.ToString("ddMMHHmm");
		string filePath = Path.Combine(outDir, $"Skalowalnosc_{timestamp}.csv");

		CsvResultWriter.WriteHeader(filePath, systemInfo);

		Console.WriteLine();
		Console.WriteLine($"Zapis do: {filePath}");
		Console.WriteLine();

		for (int t = 1; t <= 64; t++)
		{
			calc.ThreadCount = t;

			Console.Write($"Testowanie: {t,2} wątków... ");

			RenderTestResult result = runner.Run(
				calc,
				2000,
				2000,
				ViewPort.Default,
				1000,
				t);

			CsvResultWriter.Append(filePath, result);

			Console.WriteLine($"Czas: {result.RenderTimeMs,5} ms");
		}

		Console.WriteLine("\nTest zakończony. Plik zapisany pomyślnie.");
		Console.ReadKey();
	}
}