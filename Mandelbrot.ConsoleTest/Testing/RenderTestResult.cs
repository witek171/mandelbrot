namespace Mandelbrot.ConsoleTest.Testing;

public class RenderTestResult
{
	public RenderTestResult(
		string calculatorName,
		int width,
		int height,
		int maxIterations,
		int threadsUsed,
		long renderTimeMs,
		double zoomLevel,
		DateTime timestamp)
	{
		CalculatorName = calculatorName;
		Width = width;
		Height = height;
		MaxIterations = maxIterations;
		ThreadsUsed = threadsUsed;
		RenderTimeMs = renderTimeMs;
		ZoomLevel = zoomLevel;
		Timestamp = timestamp;
	}

	public string CalculatorName { get; }
	public int Width { get; }
	public int Height { get; }
	public int MaxIterations { get; }
	public int ThreadsUsed { get; }
	public long RenderTimeMs { get; }
	public double ZoomLevel { get; }
	public DateTime Timestamp { get; }
}