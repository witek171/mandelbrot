using System.Text;

namespace Mandelbrot.ConsoleTest.Testing;

public static class CsvResultWriter
{
	public static void WriteHeader(string filePath)
	{
		using StreamWriter writer = new(filePath, append: false, Encoding.UTF8);

		writer.WriteLine($"# CPU: {SystemInfo.CpuName}");
		writer.WriteLine($"# GPU: {SystemInfo.GpuName}");
		writer.WriteLine($"# Physical Cores: {SystemInfo.PhysicalCores}");
		writer.WriteLine($"# Logical Cores: {SystemInfo.LogicalCores}");
		writer.WriteLine($"# RAM: {SystemInfo.TotalRamGb:F1} GB");
		writer.WriteLine($"# OS: {SystemInfo.OsVersion}");
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