using Mandelbrot.Core.Calculators;
using Mandelbrot.Core.Rendering;

namespace Mandelbrot.ConsoleTest.Testing;

public class RenderTestRunner
{
	public RenderTestResult Run(
		IMandelbrotCalculator calculator,
		int width,
		int height,
		ViewPort viewPort,
		int maxIterations,
		int threadsUsed = 0)
	{
		IterationData data = calculator.CalculateIterations(
			width,
			height,
			viewPort,
			maxIterations);

		return new RenderTestResult(
			calculator.Name,
			width,
			height,
			maxIterations,
			threadsUsed > 0 ? threadsUsed : Environment.ProcessorCount,
			(long)data.CalculationTime.TotalMilliseconds,
			viewPort.CalculateZoomLevel(),
			DateTime.Now
		);
	}
}