using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Mandelbrot.Core
{
	/// <summary>
	/// Główna klasa odpowiedzialna za obliczenia fraktala Mandelbrota.
	/// Każde renderowanie generuje NOWY obraz na podstawie aktualnych współrzędnych ViewPort.
	/// </summary>
	public class MandelbrotCalculator
	{
		// Promień ucieczki - wartość 4 (|z|² > 4 oznacza |z| > 2)
		private const double ESCAPE_RADIUS_SQUARED = 4.0;

		// Logarytm używany do smooth coloring
		private static readonly double LOG_2 = Math.Log(2.0);

		/// <summary>
		/// Renderuje fraktal Mandelbrota w trybie jednowątkowym.
		/// KAŻDE wywołanie generuje NOWY obraz na podstawie ViewPort.
		/// </summary>
		/// <param name="width">Szerokość obrazu w pikselach</param>
		/// <param name="height">Wysokość obrazu w pikselach</param>
		/// <param name="viewPort">Obszar widoku w przestrzeni zespolonej (definiuje CO renderujemy)</param>
		/// <param name="maxIterations">Maksymalna liczba iteracji (wpływa na szczegółowość)</param>
		/// <param name="colorPalette">Paleta kolorów do użycia</param>
		/// <param name="supersampleLevel">Poziom supersamplingu (1=brak, 2=2x2, 4=4x4)</param>
		/// <returns>Wynik renderowania zawierający bitmapę i czas wykonania</returns>
		public RenderResult RenderSingleThreaded(
			int width,
			int height,
			ViewPort viewPort,
			int maxIterations,
			ColorPalette colorPalette,
			int supersampleLevel = 1)
		{
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();

			Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

			// Obliczanie skali - mapowanie pikseli na współrzędne zespolone
			// To jest KLUCZOWE - każdy piksel odpowiada KONKRETNEMU punktowi w przestrzeni zespolonej
			double xScale = viewPort.Width / width;
			double yScale = viewPort.Height / height;

			// Krok supersamplingu
			double ssStep = 1.0 / supersampleLevel;
			double ssWeight = 1.0 / (supersampleLevel * supersampleLevel);

			// Bezpośredni dostęp do pamięci bitmapy (znacznie szybszy niż SetPixel)
			BitmapData bitmapData = bitmap.LockBits(
				new Rectangle(0, 0, width, height),
				ImageLockMode.WriteOnly,
				PixelFormat.Format32bppArgb);

			int stride = bitmapData.Stride;
			byte[] pixels = new byte[height * stride];

			// Iteracja przez wszystkie piksele
			for (int py = 0; py < height; py++)
			{
				for (int px = 0; px < width; px++)
				{
					Color finalColor;

					if (supersampleLevel == 1)
					{
						// Bez supersamplingu - jeden sample na piksel
						double x0 = viewPort.MinReal + (px + 0.5) * xScale;
						double y0 = viewPort.MaxImaginary - (py + 0.5) * yScale;

						double smoothValue = CalculateSmoothIterations(x0, y0, maxIterations);
						finalColor = colorPalette.GetSmoothColor(smoothValue, maxIterations);
					}
					else
					{
						// Supersampling - uśrednianie wielu sampli
						double r = 0, g = 0, b = 0;

						for (int sy = 0; sy < supersampleLevel; sy++)
						{
							for (int sx = 0; sx < supersampleLevel; sx++)
							{
								double x0 = viewPort.MinReal + (px + (sx + 0.5) * ssStep) * xScale;
								double y0 = viewPort.MaxImaginary - (py + (sy + 0.5) * ssStep) * yScale;

								double smoothValue = CalculateSmoothIterations(x0, y0, maxIterations);
								Color sampleColor = colorPalette.GetSmoothColor(smoothValue, maxIterations);

								r += sampleColor.R * ssWeight;
								g += sampleColor.G * ssWeight;
								b += sampleColor.B * ssWeight;
							}
						}

						finalColor = Color.FromArgb(255,
							(int)Math.Min(255, r),
							(int)Math.Min(255, g),
							(int)Math.Min(255, b));
					}

					// Zapisywanie koloru do bufora
					int offset = py * stride + px * 4;
					pixels[offset] = finalColor.B;
					pixels[offset + 1] = finalColor.G;
					pixels[offset + 2] = finalColor.R;
					pixels[offset + 3] = 255;
				}
			}

			Marshal.Copy(pixels, 0, bitmapData.Scan0, pixels.Length);
			bitmap.UnlockBits(bitmapData);

			stopwatch.Stop();

			return new RenderResult
			{
				Bitmap = bitmap,
				RenderTimeMs = stopwatch.ElapsedMilliseconds,
				ThreadsUsed = 1,
				ViewPort = viewPort.Clone(),
				ZoomLevel = viewPort.CalculateZoomLevel()
			};
		}

		/// <summary>
		/// Renderuje fraktal Mandelbrota w trybie wielowątkowym z użyciem Parallel.For.
		/// Każdy wiersz jest przetwarzany przez osobny wątek.
		/// </summary>
		public RenderResult RenderMultiThreaded(
			int width,
			int height,
			ViewPort viewPort,
			int maxIterations,
			ColorPalette colorPalette,
			int supersampleLevel = 1)
		{
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();

			Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

			double xScale = viewPort.Width / width;
			double yScale = viewPort.Height / height;
			double ssStep = 1.0 / supersampleLevel;
			double ssWeight = 1.0 / (supersampleLevel * supersampleLevel);

			BitmapData bitmapData = bitmap.LockBits(
				new Rectangle(0, 0, width, height),
				ImageLockMode.WriteOnly,
				PixelFormat.Format32bppArgb);

			int stride = bitmapData.Stride;
			byte[] pixels = new byte[height * stride];

			int threadsUsed = Environment.ProcessorCount;

			// Parallel.For - każdy wiersz przetwarzany równolegle
			Parallel.For(0, height, new ParallelOptions { MaxDegreeOfParallelism = threadsUsed }, py =>
			{
				for (int px = 0; px < width; px++)
				{
					Color finalColor;

					if (supersampleLevel == 1)
					{
						double x0 = viewPort.MinReal + (px + 0.5) * xScale;
						double y0 = viewPort.MaxImaginary - (py + 0.5) * yScale;

						double smoothValue = CalculateSmoothIterations(x0, y0, maxIterations);
						finalColor = colorPalette.GetSmoothColor(smoothValue, maxIterations);
					}
					else
					{
						double r = 0, g = 0, b = 0;

						for (int sy = 0; sy < supersampleLevel; sy++)
						{
							for (int sx = 0; sx < supersampleLevel; sx++)
							{
								double x0 = viewPort.MinReal + (px + (sx + 0.5) * ssStep) * xScale;
								double y0 = viewPort.MaxImaginary - (py + (sy + 0.5) * ssStep) * yScale;

								double smoothValue = CalculateSmoothIterations(x0, y0, maxIterations);
								Color sampleColor = colorPalette.GetSmoothColor(smoothValue, maxIterations);

								r += sampleColor.R * ssWeight;
								g += sampleColor.G * ssWeight;
								b += sampleColor.B * ssWeight;
							}
						}

						finalColor = Color.FromArgb(255,
							(int)Math.Min(255, r),
							(int)Math.Min(255, g),
							(int)Math.Min(255, b));
					}

					int offset = py * stride + px * 4;
					pixels[offset] = finalColor.B;
					pixels[offset + 1] = finalColor.G;
					pixels[offset + 2] = finalColor.R;
					pixels[offset + 3] = 255;
				}
			});

			Marshal.Copy(pixels, 0, bitmapData.Scan0, pixels.Length);
			bitmap.UnlockBits(bitmapData);

			stopwatch.Stop();

			return new RenderResult
			{
				Bitmap = bitmap,
				RenderTimeMs = stopwatch.ElapsedMilliseconds,
				ThreadsUsed = threadsUsed,
				ViewPort = viewPort.Clone(),
				ZoomLevel = viewPort.CalculateZoomLevel()
			};
		}

		/// <summary>
		/// Oblicza "gładką" liczbę iteracji dla płynnego kolorowania.
		/// Używa wzoru: n + 1 - log(log|z|) / log(2)
		///
		/// To pozwala na PŁYNNE przejścia kolorów zamiast ostrych pasm.
		/// </summary>
		/// <param name="x0">Część rzeczywista punktu c</param>
		/// <param name="y0">Część urojona punktu c</param>
		/// <param name="maxIterations">Maksymalna liczba iteracji</param>
		/// <returns>Gładka wartość iteracji (może być ułamkowa)</returns>
		private double CalculateSmoothIterations(double x0, double y0, int maxIterations)
		{
			// Optymalizacja: sprawdzenie czy punkt jest w głównej kardioicie lub bańce
			// To znacząco przyspiesza renderowanie dla dużej części zbioru
			double q = (x0 - 0.25) * (x0 - 0.25) + y0 * y0;
			if (q * (q + (x0 - 0.25)) <= 0.25 * y0 * y0)
			{
				return maxIterations; // Wewnątrz głównej kardioidy
			}

			if ((x0 + 1) * (x0 + 1) + y0 * y0 <= 0.0625)
			{
				return maxIterations; // Wewnątrz głównej bańki
			}

			double x = 0.0;
			double y = 0.0;
			double x2 = 0.0;
			double y2 = 0.0;
			int iteration = 0;

			// Główna pętla iteracji
			// z(n+1) = z(n)² + c
			while (x2 + y2 <= 256.0 && iteration < maxIterations) // Używamy 256 zamiast 4 dla lepszego smooth coloring
			{
				y = 2.0 * x * y + y0;
				x = x2 - y2 + x0;
				x2 = x * x;
				y2 = y * y;
				iteration++;
			}

			if (iteration == maxIterations)
			{
				return maxIterations;
			}

			// Smooth coloring - interpolacja między iteracjami
			// Wzór: n + 1 - log₂(log₂|z|)
			double logZn = Math.Log(x2 + y2) / 2.0;
			double nu = Math.Log(logZn / LOG_2) / LOG_2;

			return iteration + 1 - nu;
		}

		/// <summary>
		/// Standardowe obliczenie iteracji (bez smooth coloring).
		/// </summary>
		public int CalculateIterations(double x0, double y0, int maxIterations)
		{
			double x = 0.0;
			double y = 0.0;
			double x2 = 0.0;
			double y2 = 0.0;
			int iteration = 0;

			while (x2 + y2 <= 4.0 && iteration < maxIterations)
			{
				y = 2.0 * x * y + y0;
				x = x2 - y2 + x0;
				x2 = x * x;
				y2 = y * y;
				iteration++;
			}

			return iteration;
		}
	}
}