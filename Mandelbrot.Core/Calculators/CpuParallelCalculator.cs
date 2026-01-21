using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Mandelbrot.Core.Rendering;

namespace Mandelbrot.Core.Calculators
{
    /// <summary>
    /// Wielowątkowy kalkulator.
    /// </summary>
    public sealed class CpuParallelCalculator : IMandelbrotCalculator
    {
        public string Name => "CPU Parallel";
        public string Description => $"🔄 Wielowątkowy ({Environment.ProcessorCount} rdzeni)";
        public bool IsAvailable => Environment.ProcessorCount > 1;

        private int _threadCount = Environment.ProcessorCount;

        public static int PhysicalCores => Environment.ProcessorCount;

        public int ThreadCount
        {
            get => _threadCount;
            set => _threadCount = Math.Max(1, value);
        }

        public bool IsOversubscribed => _threadCount > PhysicalCores;

        public IterationData CalculateIterations(int width, int height,
            ViewPort viewPort, int maxIterations)
        {
            var sw = Stopwatch.StartNew();

            int[] iterations = new int[width * height];

            double xMin = viewPort.MinReal;
            double yMax = viewPort.MaxImaginary;
            double xScale = viewPort.Width / width;
            double yScale = viewPort.Height / height;

            var options = new ParallelOptions { MaxDegreeOfParallelism = _threadCount };

            Parallel.For(0, height, options, py =>
            {
                double y0 = yMax - py * yScale;
                int rowOffset = py * width;

                for (int px = 0; px < width; px++)
                {
                    double x0 = xMin + px * xScale;
                    iterations[rowOffset + px] = CalculatePoint(x0, y0, maxIterations);
                }
            });

            sw.Stop();
            return new IterationData(iterations, width, height, maxIterations, viewPort, sw.Elapsed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CalculatePoint(double x0, double y0, int maxIterations)
        {
            // Cardioid check
            double xMinus = x0 - 0.25;
            double y2 = y0 * y0;
            double q = xMinus * xMinus + y2;
            if (q * (q + xMinus) <= 0.25 * y2)
                return maxIterations;

            // Period-2 bulb
            double xPlus = x0 + 1.0;
            if (xPlus * xPlus + y2 <= 0.0625)
                return maxIterations;

            // Główna pętla
            double x = 0, y = 0;
            double x2 = 0, y2Loop = 0;
            int iter = 0;

            while (x2 + y2Loop <= 4.0 && iter < maxIterations)
            {
                y = 2.0 * x * y + y0;
                x = x2 - y2Loop + x0;
                x2 = x * x;
                y2Loop = y * y;
                iter++;
            }

            return iter;
        }

        public void Dispose() { }
    }
}