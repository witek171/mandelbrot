namespace Mandelbrot.Core.Rendering;

public class IterationData
{
	public IterationData(int[] iterations, int width, int height,
		int maxIterations, ViewPort viewPort, TimeSpan calculationTime)
	{
		Iterations = iterations;
		Width = width;
		Height = height;
		MaxIterations = maxIterations;
		ViewPort = viewPort;
		CalculationTime = calculationTime;
	}

	public int[] Iterations { get; }
	public int Width { get; }
	public int Height { get; }
	public int MaxIterations { get; }
	public ViewPort ViewPort { get; }
	public TimeSpan CalculationTime { get; }

	public string GetCacheKey()
		=> ViewPort.GetCacheKey(Width, Height, MaxIterations);
}