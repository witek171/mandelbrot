using System.Text;

namespace Mandelbrot.ConsoleTest.Testing;

public static class CsvResultWriter
{
	public static void WriteHeader(string filePath, SystemInfo systemInfo)
	{
		using StreamWriter writer = new(filePath, append: false, Encoding.UTF8);

		writer.WriteLine($"# CPU: {systemInfo.CpuName}");
		writer.WriteLine($"# Physical Cores: {systemInfo.PhysicalCores}");
		writer.WriteLine($"# Logical Cores: {systemInfo.LogicalCores}");
		writer.WriteLine($"# RAM: {systemInfo.TotalRamGb:F1} GB");
		writer.WriteLine($"# OS: {systemInfo.OsVersion}");
		writer.WriteLine($"# Test Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
		writer.WriteLine("#");

		writer.WriteLine("Calculator,Width,Height,MaxIterations,Threads,TimeMs,ZoomLevel,Timestamp");
	}

	public static void Append(string filePath, RenderTestResult result)
	{
		using StreamWriter writer = new(filePath, append: true, Encoding.UTF8);

		writer.WriteLine(
			$"{result.CalculatorName}," +
			$"{result.Width}," +
			$"{result.Height}," +
			$"{result.MaxIterations}," +
			$"{result.ThreadsUsed}," +
			$"{result.RenderTimeMs}," +
			$"{result.ZoomLevel:G6}," +
			$"{result.Timestamp:yyyy-MM-dd HH:mm:ss}"
		);
	}
}