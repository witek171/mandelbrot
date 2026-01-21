using System.Diagnostics;
using Mandelbrot.Core.Rendering;

namespace Mandelbrot.Core.Calculators;

public sealed class CpuParallelCalculator : IMandelbrotCalculator
{
	private int _threadCount = Environment.ProcessorCount;

	public int ThreadCount
	{
		set => _threadCount = Math.Max(1, value);
	}

	public string Name => "CPU Parallel";

	public IterationData CalculateIterations(int width, int height,
		ViewPort viewPort, int maxIterations)
	{
		Stopwatch sw = Stopwatch.StartNew();

		int[] iterations = new int[width * height];

		double xMin = viewPort.MinReal;
		double yMax = viewPort.MaxImaginary;
		double xScale = viewPort.Width / width;
		double yScale = viewPort.Height / height;

		ParallelOptions options = new() { MaxDegreeOfParallelism = _threadCount };

		Parallel.For(0, height, options, py =>
		{
			double y0 = yMax - py * yScale;
			int rowOffset = py * width;
			for (int px = 0; px < width; px++)
			{
				double x0 = xMin + px * xScale;
				iterations[rowOffset + px] = MandelbrotMath.CalculatePoint(x0, y0, maxIterations);
			}
		});

		sw.Stop();
		return new IterationData(iterations, width, height, maxIterations, viewPort, sw.Elapsed);
	}

	public void Dispose()
	{
	}
}