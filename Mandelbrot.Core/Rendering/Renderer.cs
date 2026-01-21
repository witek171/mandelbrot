using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using Mandelbrot.Core.Calculators;
using Mandelbrot.Core.Pooling;

namespace Mandelbrot.Core.Rendering;

public class Renderer : IDisposable
{
	private readonly BitmapPool _bitmapPool;
	private readonly IterationCache _cache;
	private readonly object _lock = new();
	private Bitmap _lastBitmap;

	private IterationData _lastIterations;

	public Renderer(IterationCache cache, BitmapPool bitmapPool)
	{
		_cache = cache;
		_bitmapPool = bitmapPool;
	}

	public void Dispose()
	{
	}

	public RenderResult Render(
		IMandelbrotCalculator calculator,
		int width,
		int height,
		ViewPort viewPort,
		int maxIterations,
		ColorPalette palette)
	{
		Stopwatch sw = Stopwatch.StartNew();

		string cacheKey = viewPort.GetCacheKey(width, height, maxIterations);
		IterationData iterations;

		if (_cache.TryGet(cacheKey, out IterationData cached))
		{
			iterations = cached;
		}
		else
		{
			iterations = calculator.CalculateIterations(width, height, viewPort, maxIterations);
			_cache.Add(iterations);
		}

		Bitmap bitmap = ColorizeIterations(iterations, palette);

		sw.Stop();

		lock (_lock)
		{
			_lastIterations = iterations;
			_lastBitmap = bitmap;
		}

		return new RenderResult(bitmap, sw.Elapsed, viewPort.CalculateZoomLevel());
	}

	public RenderResult RecolorWithPalette(ColorPalette palette)
	{
		lock (_lock)
		{
			if (_lastIterations == null)
				return null;

			Stopwatch sw = Stopwatch.StartNew();

			Bitmap bitmap = ColorizeIterations(_lastIterations, palette);
			_lastBitmap = bitmap;

			sw.Stop();

			return new RenderResult(
				bitmap,
				sw.Elapsed,
				_lastIterations.ViewPort.CalculateZoomLevel());
		}
	}

	private Bitmap ColorizeIterations(IterationData data, ColorPalette palette)
	{
		int width = data.Width;
		int height = data.Height;
		int maxIter = data.MaxIterations;
		int[] iterations = data.Iterations;

		Bitmap bitmap = _bitmapPool.Rent(width, height);

		Rectangle rect = new(0, 0, width, height);
		BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

		try
		{
			unsafe
			{
				int* pixels = (int*)bmpData.Scan0;

				Parallel.For(0, height, py =>
				{
					int rowOffset = py * width;
					for (int px = 0; px < width; px++)
					{
						int idx = rowOffset + px;
						pixels[idx] = palette.GetColorArgb(iterations[idx], maxIter);
					}
				});
			}
		}
		finally
		{
			bitmap.UnlockBits(bmpData);
		}

		return bitmap;
	}
}