using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Mandelbrot.Core.Calculators
{
    /// <summary>
    /// Podstawowa implementacja CPU jednowątkowa (referencyjna)
    /// </summary>
    public class CpuSingleThreadCalculator : IMandelbrotCalculator
    {
        public string Name => "CPU Jednowątkowy";
        public bool IsAvailable => true;

        public RenderResult Render(int width, int height, ViewPort viewPort,
            int maxIterations, ColorPalette colorPalette)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            double xScale = viewPort.Width / width;
            double yScale = viewPort.Height / height;

            BitmapData bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            int stride = bitmapData.Stride;
            byte[] pixels = new byte[height * stride];

            for (int py = 0; py < height; py++)
            {
                double y0 = viewPort.MaxImaginary - (py + 0.5) * yScale;

                for (int px = 0; px < width; px++)
                {
                    double x0 = viewPort.MinReal + (px + 0.5) * xScale;

                    double smoothValue = CalculateSmoothIterations(x0, y0, maxIterations);
                    Color color = smoothValue >= maxIterations
                        ? Color.Black
                        : colorPalette.GetSmoothColor(smoothValue, maxIterations);

                    int offset = py * stride + px * 4;
                    pixels[offset] = color.B;
                    pixels[offset + 1] = color.G;
                    pixels[offset + 2] = color.R;
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
                CalculatorName = Name,
                ViewPort = viewPort.Clone(),
                ZoomLevel = viewPort.CalculateZoomLevel()
            };
        }

        private double CalculateSmoothIterations(double x0, double y0, int maxIterations)
        {
            // Optymalizacja kardioidy
            double q = (x0 - 0.25) * (x0 - 0.25) + y0 * y0;
            if (q * (q + (x0 - 0.25)) <= 0.25 * y0 * y0)
                return maxIterations;

            if ((x0 + 1) * (x0 + 1) + y0 * y0 <= 0.0625)
                return maxIterations;

            double x = 0, y = 0, x2 = 0, y2 = 0;
            int iteration = 0;

            while (x2 + y2 <= 256 && iteration < maxIterations)
            {
                y = 2 * x * y + y0;
                x = x2 - y2 + x0;
                x2 = x * x;
                y2 = y * y;
                iteration++;
            }

            if (iteration >= maxIterations)
                return maxIterations;

            double logZn = Math.Log(x2 + y2) / 2;
            double nu = Math.Log(logZn / Math.Log(2)) / Math.Log(2);
            return iteration + 1 - nu;
        }

        public void Dispose() { }
    }

    /// <summary>
    /// Implementacja CPU wielowątkowa z Parallel.For
    /// </summary>
    public class CpuParallelCalculator : IMandelbrotCalculator
    {
        public string Name => $"CPU Parallel ({Environment.ProcessorCount} wątków)";
        public bool IsAvailable => true;

        public RenderResult Render(int width, int height, ViewPort viewPort,
            int maxIterations, ColorPalette colorPalette)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            double xScale = viewPort.Width / width;
            double yScale = viewPort.Height / height;

            BitmapData bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            int stride = bitmapData.Stride;
            byte[] pixels = new byte[height * stride];

            Parallel.For(0, height, py =>
            {
                double y0 = viewPort.MaxImaginary - (py + 0.5) * yScale;

                for (int px = 0; px < width; px++)
                {
                    double x0 = viewPort.MinReal + (px + 0.5) * xScale;

                    double smoothValue = CalculateSmoothIterations(x0, y0, maxIterations);
                    Color color = smoothValue >= maxIterations
                        ? Color.Black
                        : colorPalette.GetSmoothColor(smoothValue, maxIterations);

                    int offset = py * stride + px * 4;
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
                ThreadsUsed = Environment.ProcessorCount,
                CalculatorName = Name,
                ViewPort = viewPort.Clone(),
                ZoomLevel = viewPort.CalculateZoomLevel()
            };
        }

        private double CalculateSmoothIterations(double x0, double y0, int maxIterations)
        {
            double q = (x0 - 0.25) * (x0 - 0.25) + y0 * y0;
            if (q * (q + (x0 - 0.25)) <= 0.25 * y0 * y0)
                return maxIterations;

            if ((x0 + 1) * (x0 + 1) + y0 * y0 <= 0.0625)
                return maxIterations;

            double x = 0, y = 0, x2 = 0, y2 = 0;
            int iteration = 0;

            while (x2 + y2 <= 256 && iteration < maxIterations)
            {
                y = 2 * x * y + y0;
                x = x2 - y2 + x0;
                x2 = x * x;
                y2 = y * y;
                iteration++;
            }

            if (iteration >= maxIterations)
                return maxIterations;

            double logZn = Math.Log(x2 + y2) / 2;
            double nu = Math.Log(logZn / Math.Log(2)) / Math.Log(2);
            return iteration + 1 - nu;
        }

        public void Dispose() { }
    }
}