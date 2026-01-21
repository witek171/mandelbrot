using Mandelbrot.Core.Rendering;

namespace Mandelbrot.Core.Calculators;

public interface IMandelbrotCalculator : IDisposable
{
	string Name { get; }

	IterationData CalculateIterations(
		int width,
		int height,
		ViewPort viewPort,
		int maxIterations);
}