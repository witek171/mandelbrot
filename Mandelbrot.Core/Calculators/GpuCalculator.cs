using System;
using System.Diagnostics;
using System.Linq;
using Cloo;
using Mandelbrot.Core.Rendering;

namespace Mandelbrot.Core.Calculators
{
    /// <summary>
    /// GPU kalkulator z OpenCL.
    /// Działa na każdej karcie graficznej.
    /// </summary>
    public sealed class GpuCalculator : IMandelbrotCalculator
    {
        public string Name => "GPU (OpenCL)";
        public string Description { get; private set; }
        public bool IsAvailable { get; private set; }

        private ComputeContext _context;
        private ComputeCommandQueue _queue;
        private ComputeProgram _program;
        private ComputeKernel _kernel;
        private ComputeDevice _device;
        private bool _supportsDouble;
        private int[] _workSize;

        private const string KernelDouble = @"
            #pragma OPENCL EXTENSION cl_khr_fp64 : enable
            
            __kernel void mandelbrot(
                __global int* output,
                const double xMin,
                const double yMax,
                const double xScale,
                const double yScale,
                const int width,
                const int maxIter)
            {
                int px = get_global_id(0);
                int py = get_global_id(1);
                if (px >= width) return;
                
                double x0 = xMin + px * xScale;
                double y0 = yMax - py * yScale;
                
                // Cardioid check
                double xMinus = x0 - 0.25;
                double y2 = y0 * y0;
                double q = xMinus * xMinus + y2;
                if (q * (q + xMinus) <= 0.25 * y2) {
                    output[py * width + px] = maxIter;
                    return;
                }
                
                // Period-2 bulb
                double xPlus = x0 + 1.0;
                if (xPlus * xPlus + y2 <= 0.0625) {
                    output[py * width + px] = maxIter;
                    return;
                }
                
                double x = 0.0, y = 0.0;
                double x2 = 0.0, y2Loop = 0.0;
                int iter = 0;
                
                while (x2 + y2Loop <= 4.0 && iter < maxIter) {
                    y = 2.0 * x * y + y0;
                    x = x2 - y2Loop + x0;
                    x2 = x * x;
                    y2Loop = y * y;
                    iter++;
                }
                
                output[py * width + px] = iter;
            }";

        private const string KernelFloat = @"
            __kernel void mandelbrot(
                __global int* output,
                const float xMin,
                const float yMax,
                const float xScale,
                const float yScale,
                const int width,
                const int maxIter)
            {
                int px = get_global_id(0);
                int py = get_global_id(1);
                if (px >= width) return;
                
                float x0 = xMin + px * xScale;
                float y0 = yMax - py * yScale;
                
                float xMinus = x0 - 0.25f;
                float y2 = y0 * y0;
                float q = xMinus * xMinus + y2;
                if (q * (q + xMinus) <= 0.25f * y2) {
                    output[py * width + px] = maxIter;
                    return;
                }
                
                float xPlus = x0 + 1.0f;
                if (xPlus * xPlus + y2 <= 0.0625f) {
                    output[py * width + px] = maxIter;
                    return;
                }
                
                float x = 0.0f, y = 0.0f;
                float x2 = 0.0f, y2Loop = 0.0f;
                int iter = 0;
                
                while (x2 + y2Loop <= 4.0f && iter < maxIter) {
                    y = 2.0f * x * y + y0;
                    x = x2 - y2Loop + x0;
                    x2 = x * x;
                    y2Loop = y * y;
                    iter++;
                }
                
                output[py * width + px] = iter;
            }";

        public GpuCalculator()
        {
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                ComputeDevice best = null;
                ComputePlatform bestPlatform = null;
                int bestScore = -1;

                foreach (var platform in ComputePlatform.Platforms)
                {
                    foreach (var device in platform.Devices)
                    {
                        int score = ScoreDevice(device);
                        if (score > bestScore)
                        {
                            bestScore = score;
                            best = device;
                            bestPlatform = platform;
                        }
                    }
                }

                if (best == null)
                {
                    Console.WriteLine("  ⚠ Brak urządzeń OpenCL");
                    IsAvailable = false;
                    return;
                }

                _device = best;
                _supportsDouble = _device.Extensions.Any(e =>
                    e.Contains("cl_khr_fp64") || e.Contains("cl_amd_fp64"));

                var props = new ComputeContextPropertyList(bestPlatform);
                _context = new ComputeContext(new[] { _device }, props, null, IntPtr.Zero);
                _queue = new ComputeCommandQueue(_context, _device, ComputeCommandQueueFlags.None);

                string source = _supportsDouble ? KernelDouble : KernelFloat;
                _program = new ComputeProgram(_context, source);

                try
                {
                    _program.Build(null, "-cl-fast-relaxed-math", null, IntPtr.Zero);
                }
                catch (BuildProgramFailureComputeException)
                {
                    string log = _program.GetBuildLog(_device);
                    throw new Exception($"Błąd kompilacji:\n{log}");
                }

                _kernel = _program.CreateKernel("mandelbrot");

                _workSize = _device.MaxWorkGroupSize >= 256 ? new[] { 16, 16 } :
                            _device.MaxWorkGroupSize >= 64 ? new[] { 8, 8 } : new[] { 4, 4 };

                string precision = _supportsDouble ? "64-bit" : "32-bit";
                Description = $"🎮 {_device.Name.Trim()} ({precision})";
                IsAvailable = true;

                Console.WriteLine($"  GPU: {_device.Name.Trim()} ({precision})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ⚠ GPU Error: {ex.Message}");
                IsAvailable = false;
            }
        }

        private int ScoreDevice(ComputeDevice d)
        {
            int score = 0;
            if (d.Type == ComputeDeviceTypes.Gpu) score += 1000;
            score += (int)d.MaxComputeUnits * 10;
            if (d.GlobalMemorySize > 1024L * 1024 * 1024) score += 500;
            return score;
        }

        public IterationData CalculateIterations(int width, int height,
            ViewPort viewPort, int maxIterations)
        {
            if (!IsAvailable)
                throw new InvalidOperationException("GPU niedostępne");

            var sw = Stopwatch.StartNew();

            int[] iterations = new int[width * height];
            double xScale = viewPort.Width / width;
            double yScale = viewPort.Height / height;

            using (var buffer = new ComputeBuffer<int>(
                _context, ComputeMemoryFlags.WriteOnly, iterations.Length))
            {
                if (_supportsDouble)
                {
                    _kernel.SetMemoryArgument(0, buffer);
                    _kernel.SetValueArgument(1, viewPort.MinReal);
                    _kernel.SetValueArgument(2, viewPort.MaxImaginary);
                    _kernel.SetValueArgument(3, xScale);
                    _kernel.SetValueArgument(4, yScale);
                    _kernel.SetValueArgument(5, width);
                    _kernel.SetValueArgument(6, maxIterations);
                }
                else
                {
                    _kernel.SetMemoryArgument(0, buffer);
                    _kernel.SetValueArgument(1, (float)viewPort.MinReal);
                    _kernel.SetValueArgument(2, (float)viewPort.MaxImaginary);
                    _kernel.SetValueArgument(3, (float)xScale);
                    _kernel.SetValueArgument(4, (float)yScale);
                    _kernel.SetValueArgument(5, width);
                    _kernel.SetValueArgument(6, maxIterations);
                }

                long[] global = { RoundUp(width, _workSize[0]), RoundUp(height, _workSize[1]) };
                long[] local = { _workSize[0], _workSize[1] };

                _queue.Execute(_kernel, null, global, local, null);
                _queue.ReadFromBuffer(buffer, ref iterations, true, null);
            }

            sw.Stop();
            return new IterationData(iterations, width, height, maxIterations, viewPort, sw.Elapsed);
        }

        private static long RoundUp(int v, int m) => ((v + m - 1) / m) * m;

        public void Dispose()
        {
            _kernel?.Dispose();
            _program?.Dispose();
            _queue?.Dispose();
            _context?.Dispose();
        }
    }
}