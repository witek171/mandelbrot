using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Mandelbrot.Core.Calculators
{
    /// <summary>
    /// Zoptymalizowana wersja CPU - szybka i stabilna.
    /// Używa wszystkich dostępnych optymalizacji bez GPU.
    /// </summary>
    public class CpuOptimizedCalculator : IMandelbrotCalculator
    {
        private readonly int _threadCount;

        public string Name => $"CPU Optimized ({_threadCount} wątków)";
        public bool IsAvailable => true;

        public CpuOptimizedCalculator()
        {
            _threadCount = Environment.ProcessorCount;
        }

        public RenderResult Render(
            int width,
            int height,
            ViewPort viewPort,
            int maxIterations,
            ColorPalette colorPalette)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Pre-generowanie palety kolorów
            Color[] palette = colorPalette.GeneratePalette(Math.Max(2048, maxIterations));

            // Alokacja bufora
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            BitmapData bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            int stride = bitmapData.Stride;
            byte[] pixels = new byte[height * stride];

            // Parametry
            double xScale = viewPort.Width / width;
            double yScale = viewPort.Height / height;
            double minReal = viewPort.MinReal;
            double maxImaginary = viewPort.MaxImaginary;

            // Przetwarzanie równoległe
            Parallel.For(0, height, new ParallelOptions { MaxDegreeOfParallelism = _threadCount }, py =>
            {
                double y0 = maxImaginary - (py + 0.5) * yScale;
                int rowOffset = py * stride;

                for (int px = 0; px < width; px++)
                {
                    double x0 = minReal + (px + 0.5) * xScale;

                    // Obliczanie smooth iterations
                    double smoothIter = CalculateSmoothIterations(x0, y0, maxIterations);

                    // Kolorowanie
                    Color color;
                    if (smoothIter >= maxIterations)
                    {
                        color = Color.Black;
                    }
                    else
                    {
                        // Interpolacja koloru
                        double colorIdx = smoothIter * 3.0;
                        int idx = ((int)colorIdx) % palette.Length;
                        int idx2 = (idx + 1) % palette.Length;
                        double frac = colorIdx - Math.Floor(colorIdx);

                        Color c1 = palette[idx];
                        Color c2 = palette[idx2];

                        int r = (int)(c1.R + (c2.R - c1.R) * frac);
                        int g = (int)(c1.G + (c2.G - c1.G) * frac);
                        int b = (int)(c1.B + (c2.B - c1.B) * frac);

                        color = Color.FromArgb(255,
                            Math.Clamp(r, 0, 255),
                            Math.Clamp(g, 0, 255),
                            Math.Clamp(b, 0, 255));
                    }

                    int offset = rowOffset + px * 4;
                    pixels[offset] = color.B;
                    pixels[offset + 1] = color.G;
                    pixels[offset + 2] = color.R;
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
                ThreadsUsed = _threadCount,
                CalculatorName = Name,
                ViewPort = viewPort.Clone(),
                ZoomLevel = viewPort.CalculateZoomLevel()
            };
        }

        /// <summary>
        /// Oblicza smooth iterations z optymalizacjami.
        /// AggressiveInlining = kompilator wstawia kod bezpośrednio (szybciej)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double CalculateSmoothIterations(double x0, double y0, int maxIterations)
        {
            // Optymalizacja 1: Test kardioidy
            // Punkty w kardioicie zawsze należą do zbioru
            double q = (x0 - 0.25) * (x0 - 0.25) + y0 * y0;
            if (q * (q + (x0 - 0.25)) <= 0.25 * y0 * y0)
            {
                return maxIterations;
            }

            // Optymalizacja 2: Test głównej bańki (period-2 bulb)
            double x1 = x0 + 1.0;
            if (x1 * x1 + y0 * y0 <= 0.0625)
            {
                return maxIterations;
            }

            // Główna pętla
            double x = 0.0;
            double y = 0.0;
            double x2 = 0.0;
            double y2 = 0.0;
            int iteration = 0;

            // Używamy 256 jako escape radius dla lepszego smooth coloring
            while (x2 + y2 <= 256.0 && iteration < maxIterations)
            {
                y = 2.0 * x * y + y0;
                x = x2 - y2 + x0;
                x2 = x * x;
                y2 = y * y;
                iteration++;
            }

            if (iteration >= maxIterations)
            {
                return maxIterations;
            }

            // Smooth coloring: n + 1 - log2(log2|z|)
            double logZn = Math.Log(x2 + y2) * 0.5;
            double nu = Math.Log(logZn / 0.693147180559945) / 0.693147180559945; // log(2)

            return iteration + 1.0 - nu;
        }

        public void Dispose() { }
    }
}