using System;
using Mandelbrot.Core.Rendering;

namespace Mandelbrot.Core.Calculators
{
	public interface IMandelbrotCalculator : IDisposable
	{
		string Name { get; }
		string Description { get; }
		bool IsAvailable { get; }

		IterationData CalculateIterations(
			int width,
			int height,
			ViewPort viewPort,
			int maxIterations);
	}
}