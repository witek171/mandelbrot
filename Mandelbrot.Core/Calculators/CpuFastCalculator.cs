using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Mandelbrot.Core.Calculators
{
    /// <summary>
    /// Zoptymalizowana wersja CPU - bez skomplikowanego SIMD.
    /// Używa prostych optymalizacji które DZIAŁAJĄ szybko.
    /// </summary>
    public class CpuFastCalculator : IMandelbrotCalculator
    {
        // ZMIANA 1: Usunięto 'readonly', żeby można było zmieniać liczbę wątków
        private int _threadCount;

        // ZMIANA 2: Właściwość do sterowania wątkami z zewnątrz (MainForm)
        public int ThreadCount
        {
            get => _threadCount;
            set
            {
                // Jeśli ktoś poda < 1 (np. błąd), ustawiamy automat
                if (value < 1)
                    _threadCount = Environment.ProcessorCount;
                else
                    _threadCount = value;
            }
        }

        public string Name => $"CPU Fast ({_threadCount} wątków)";
        public bool IsAvailable => true;

        public CpuFastCalculator()
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

            // Pre-alokacja wszystkiego
            int totalPixels = width * height;
            int[] iterationData = new int[totalPixels];
            double[] smoothData = new double[totalPixels];

            double xScale = viewPort.Width / width;
            double yScale = viewPort.Height / height;
            double minReal = viewPort.MinReal;
            double maxImaginary = viewPort.MaxImaginary;

            // Faza 1: Obliczenia (czyste math, bez kolorowania)
            // Tutaj 'Parallel.For' użyje naszej zaktualizowanej liczby wątków (_threadCount)
            Parallel.For(0, height, new ParallelOptions { MaxDegreeOfParallelism = _threadCount }, py =>
            {
                double y0 = maxImaginary - (py + 0.5) * yScale;
                int rowOffset = py * width;

                for (int px = 0; px < width; px++)
                {
                    double x0 = minReal + (px + 0.5) * xScale;

                    int idx = rowOffset + px;
                    int idx_iter;
                    double idx_smooth;
                    // Mała zmiana w wywołaniu metody (C# wymaga zmiennych lokalnych dla out w lambda)
                    CalculatePoint(x0, y0, maxIterations, out idx_iter, out idx_smooth);
                    iterationData[idx] = idx_iter;
                    smoothData[idx] = idx_smooth;
                }
            });

            // Faza 2: Kolorowanie (osobno - lepsze cache usage)
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            BitmapData bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            int stride = bitmapData.Stride;
            byte[] pixels = new byte[height * stride];

            // Pre-generuj paletę
            Color[] palette = colorPalette.GeneratePalette(2048);
            int paletteLength = palette.Length;

            // Tutaj też używamy ograniczenia wątków!
            Parallel.For(0, height, new ParallelOptions { MaxDegreeOfParallelism = _threadCount }, py =>
            {
                int rowOffset = py * width;
                int pixelRowOffset = py * stride;

                for (int px = 0; px < width; px++)
                {
                    int idx = rowOffset + px;
                    int iter = iterationData[idx];
                    double smooth = smoothData[idx];

                    int offset = pixelRowOffset + px * 4;

                    if (iter >= maxIterations)
                    {
                        // Czarny
                        pixels[offset] = 0;
                        pixels[offset + 1] = 0;
                        pixels[offset + 2] = 0;
                        pixels[offset + 3] = 255;
                    }
                    else
                    {
                        // Interpolacja kolorów
                        double colorIdx = smooth * 3.0;
                        int ci = ((int)colorIdx) % paletteLength;
                        int ci2 = (ci + 1) % paletteLength;
                        double frac = colorIdx - Math.Floor(colorIdx);

                        Color c1 = palette[ci];
                        Color c2 = palette[ci2];

                        pixels[offset] = (byte)(c1.B + (c2.B - c1.B) * frac);
                        pixels[offset + 1] = (byte)(c1.G + (c2.G - c1.G) * frac);
                        pixels[offset + 2] = (byte)(c1.R + (c2.R - c1.R) * frac);
                        pixels[offset + 3] = 255;
                    }
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
        /// Obliczenie pojedynczego punktu - maksymalnie zoptymalizowane.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void CalculatePoint(double x0, double y0, int maxIterations,
            out int iterations, out double smooth)
        {
            // Optymalizacja: test kardioidy
            double q = (x0 - 0.25) * (x0 - 0.25) + y0 * y0;
            if (q * (q + (x0 - 0.25)) <= 0.25 * y0 * y0)
            {
                iterations = maxIterations;
                smooth = maxIterations;
                return;
            }

            // Optymalizacja: test period-2 bulb
            double xp1 = x0 + 1.0;
            if (xp1 * xp1 + y0 * y0 <= 0.0625)
            {
                iterations = maxIterations;
                smooth = maxIterations;
                return;
            }

            double x = 0.0;
            double y = 0.0;
            double x2 = 0.0;
            double y2 = 0.0;
            int iter = 0;

            // Escape radius = 256 dla lepszego smooth coloring
            while (x2 + y2 <= 256.0 && iter < maxIterations)
            {
                y = 2.0 * x * y + y0;
                x = x2 - y2 + x0;
                x2 = x * x;
                y2 = y * y;
                iter++;
            }

            iterations = iter;

            if (iter >= maxIterations)
            {
                smooth = maxIterations;
            }
            else
            {
                // Smooth coloring formula
                double logZn = Math.Log(x2 + y2) * 0.5;
                double nu = Math.Log(logZn * 1.4426950408889634) * 1.4426950408889634; // 1/ln(2)
                smooth = iter + 1.0 - nu;
            }
        }

        public void Dispose() { }
    }
}