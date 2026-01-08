using System.Drawing;

namespace Mandelbrot.Core.Calculators
{
	/// <summary>
	/// Interfejs dla wszystkich implementacji kalkulatora Mandelbrota.
	/// Pozwala na łatwe przełączanie między CPU/GPU.
	/// </summary>
	public interface IMandelbrotCalculator : IDisposable
	{
		/// <summary>
		/// Nazwa implementacji (do wyświetlenia w UI)
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Czy ta implementacja jest dostępna na tym systemie
		/// </summary>
		bool IsAvailable { get; }

		/// <summary>
		/// Renderuje fraktal Mandelbrota
		/// </summary>
		RenderResult Render(
			int width,
			int height,
			ViewPort viewPort,
			int maxIterations,
			ColorPalette colorPalette);
	}
}