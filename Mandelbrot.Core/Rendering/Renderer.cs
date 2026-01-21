// Mandelbrot.Core/Rendering/Renderer.cs
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using Mandelbrot.Core.Caching;
using Mandelbrot.Core.Calculators;
using Mandelbrot.Core.Pooling;

namespace Mandelbrot.Core.Rendering
{
    /// <summary>
    /// Główny renderer - łączy obliczenia z kolorowaniem.
    /// Obsługuje cache i instant zmianę palety.
    /// </summary>
    public class Renderer : IDisposable
    {
        private readonly IterationCache _cache;
        private readonly BitmapPool _bitmapPool;

        // Ostatnio użyte dane - dla instant zmiany palety
        private IterationData _lastIterations;
        private Bitmap _lastBitmap;
        private readonly object _lock = new();

        public Renderer(IterationCache cache, BitmapPool bitmapPool)
        {
            _cache = cache;
            _bitmapPool = bitmapPool;
        }

        /// <summary>
        /// Pełne renderowanie (obliczenia + kolorowanie)
        /// </summary>
        public RenderResult Render(
            IMandelbrotCalculator calculator,
            int width,
            int height,
            ViewPort viewPort,
            int maxIterations,
            ColorPalette palette)
        {
            var sw = Stopwatch.StartNew();

            // Sprawdź cache
            string cacheKey = viewPort.GetCacheKey(width, height, maxIterations);
            IterationData iterations;

            if (_cache.TryGet(cacheKey, out var cached))
            {
                iterations = cached;
            }
            else
            {
                // Oblicz iteracje
                iterations = calculator.CalculateIterations(width, height, viewPort, maxIterations);
                _cache.Add(iterations);
            }

            // Kolorowanie
            var bitmap = ColorizeIterations(iterations, palette);

            sw.Stop();

            // Zapisz jako ostatnie (dla instant zmiany palety)
            lock (_lock)
            {
                _lastIterations = iterations;
                _lastBitmap = bitmap;
            }

            return new RenderResult(bitmap, sw.Elapsed, viewPort.CalculateZoomLevel());
        }

        /// <summary>
        /// INSTANT zmiana palety - tylko re-kolorowanie, bez obliczeń!
        /// </summary>
        public RenderResult RecolorWithPalette(ColorPalette palette)
        {
            lock (_lock)
            {
                if (_lastIterations == null)
                    return null;

                var sw = Stopwatch.StartNew();

                var bitmap = ColorizeIterations(_lastIterations, palette);
                _lastBitmap = bitmap;

                sw.Stop();

                return new RenderResult(
                    bitmap,
                    sw.Elapsed,
                    _lastIterations.ViewPort.CalculateZoomLevel());
            }
        }

        /// <summary>
        /// Koloruje iteracje do bitmapy
        /// </summary>
        private Bitmap ColorizeIterations(IterationData data, ColorPalette palette)
        {
            int width = data.Width;
            int height = data.Height;
            int maxIter = data.MaxIterations;
            int[] iterations = data.Iterations;

            var bitmap = _bitmapPool.Rent(width, height);

            var rect = new Rectangle(0, 0, width, height);
            var bmpData = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            try
            {
                unsafe
                {
                    int* pixels = (int*)bmpData.Scan0;

                    // Równoległe kolorowanie
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

        public void Dispose()
        {
            // Bitmap pool zarządza bitmapami
        }
    }
}