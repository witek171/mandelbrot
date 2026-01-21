// Mandelbrot.Core/Rendering/IterationData.cs
using System;

namespace Mandelbrot.Core.Rendering
{
	/// <summary>
	/// Przechowuje wyniki obliczeń iteracji (bez kolorów).
	/// Pozwala na instant zmianę palety bez przeliczania!
	/// </summary>
	public class IterationData
	{
		public int[] Iterations { get; }
		public int Width { get; }
		public int Height { get; }
		public int MaxIterations { get; }
		public ViewPort ViewPort { get; }
		public TimeSpan CalculationTime { get; }

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

		public string GetCacheKey()
		{
			return ViewPort.GetCacheKey(Width, Height, MaxIterations);
		}
	}
}