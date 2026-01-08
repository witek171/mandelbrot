using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Cloo;

namespace Mandelbrot.Core.Calculators
{
    /// <summary>
    /// Hybrydowy kalkulator GPU/CPU.
    /// - Używa GPU (float) dla zoomu < 100,000x (szybko!)
    /// - Przełącza na CPU (double) dla głębszego zoomu (dokładnie!)
    /// </summary>
    public class GpuHybridCalculator : IMandelbrotCalculator
    {
        // Próg zoomu - powyżej tego GPU float traci precyzję
        private const double GPU_ZOOM_LIMIT = 100_000.0;

        private readonly CpuFastCalculator _cpuCalculator;

        // OpenCL
        private ComputePlatform _platform;
        private ComputeContext _context;
        private ComputeCommandQueue _queue;
        private ComputeProgram _program;
        private ComputeKernel _kernel;
        private ComputeDevice _device;
        private string _deviceName = "niedostępne";
        private bool _gpuAvailable = false;

        private const string KernelSource = @"
            __kernel void mandelbrot(
                __global int* iterations,
                __global float* smooth,
                const int width,
                const int height,
                const float minReal,
                const float maxImaginary,
                const float xScale,
                const float yScale,
                const int maxIterations)
            {
                int px = get_global_id(0);
                int py = get_global_id(1);
                
                if (px >= width || py >= height)
                    return;
                
                float x0 = minReal + (float)px * xScale;
                float y0 = maxImaginary - (float)py * yScale;
                
                // Kardioida test
                float q = (x0 - 0.25f) * (x0 - 0.25f) + y0 * y0;
                if (q * (q + (x0 - 0.25f)) <= 0.25f * y0 * y0)
                {
                    iterations[py * width + px] = maxIterations;
                    smooth[py * width + px] = (float)maxIterations;
                    return;
                }
                
                // Period-2 bulb test
                float xp1 = x0 + 1.0f;
                if (xp1 * xp1 + y0 * y0 <= 0.0625f)
                {
                    iterations[py * width + px] = maxIterations;
                    smooth[py * width + px] = (float)maxIterations;
                    return;
                }
                
                float x = 0.0f;
                float y = 0.0f;
                float x2 = 0.0f;
                float y2 = 0.0f;
                int iter = 0;
                
                while (x2 + y2 <= 256.0f && iter < maxIterations)
                {
                    y = 2.0f * x * y + y0;
                    x = x2 - y2 + x0;
                    x2 = x * x;
                    y2 = y * y;
                    iter++;
                }
                
                iterations[py * width + px] = iter;
                
                if (iter >= maxIterations)
                {
                    smooth[py * width + px] = (float)maxIterations;
                }
                else
                {
                    float logZn = log(x2 + y2) * 0.5f;
                    float nu = log(logZn * 1.4426950408889634f) * 1.4426950408889634f;
                    smooth[py * width + px] = (float)iter + 1.0f - nu;
                }
            }
        ";

        public string Name
        {
            get
            {
                if (_gpuAvailable)
                    return $"GPU/CPU Hybrid ({_deviceName})";
                else
                    return "CPU Fast (GPU niedostępne)";
            }
        }

        public bool IsAvailable => true; // Zawsze - fallback do CPU

        public GpuHybridCalculator()
        {
            _cpuCalculator = new CpuFastCalculator();
            InitializeGpu();
        }

        private void InitializeGpu()
        {
            try
            {
                var platforms = ComputePlatform.Platforms;
                if (platforms.Count == 0) return;

                ComputeDevice selectedDevice = null;

                foreach (var platform in platforms)
                {
                    foreach (var device in platform.Devices)
                    {
                        if (device.Type == ComputeDeviceTypes.Gpu)
                        {
                            selectedDevice = device;
                            _platform = platform;
                            break;
                        }
                    }
                    if (selectedDevice != null) break;
                }

                // Fallback do Intel CPU OpenCL
                if (selectedDevice == null)
                {
                    foreach (var platform in platforms)
                    {
                        if (platform.Vendor.ToLower().Contains("intel"))
                        {
                            foreach (var device in platform.Devices)
                            {
                                selectedDevice = device;
                                _platform = platform;
                                break;
                            }
                        }
                        if (selectedDevice != null) break;
                    }
                }

                if (selectedDevice == null) return;

                _device = selectedDevice;
                _deviceName = _device.Name.Trim();

                var properties = new ComputeContextPropertyList(_platform);
                _context = new ComputeContext(new[] { _device }, properties, null, IntPtr.Zero);
                _queue = new ComputeCommandQueue(_context, _device, ComputeCommandQueueFlags.None);

                _program = new ComputeProgram(_context, KernelSource);
                _program.Build(new[] { _device }, "-cl-fast-relaxed-math", null, IntPtr.Zero);
                _kernel = _program.CreateKernel("mandelbrot");

                _gpuAvailable = true;
                Console.WriteLine($"GPU Hybrid: {_deviceName} (limit zoomu: {GPU_ZOOM_LIMIT:N0}x)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GPU init failed: {ex.Message}");
                _gpuAvailable = false;
            }
        }

        public RenderResult Render(
            int width,
            int height,
            ViewPort viewPort,
            int maxIterations,
            ColorPalette colorPalette)
        {
            double zoomLevel = viewPort.CalculateZoomLevel();

            // Automatyczne przełączanie!
            if (!_gpuAvailable || zoomLevel > GPU_ZOOM_LIMIT)
            {
                // Użyj CPU dla głębokiego zoomu lub gdy GPU niedostępne
                var cpuResult = _cpuCalculator.Render(width, height, viewPort, maxIterations, colorPalette);

                // Zmień nazwę żeby było widać
                cpuResult.CalculatorName = zoomLevel > GPU_ZOOM_LIMIT
                    ? $"CPU (zoom {zoomLevel:G3}x > limit)"
                    : "CPU Fast";

                return cpuResult;
            }

            // GPU rendering
            return RenderGpu(width, height, viewPort, maxIterations, colorPalette);
        }

        private RenderResult RenderGpu(
            int width,
            int height,
            ViewPort viewPort,
            int maxIterations,
            ColorPalette colorPalette)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            int totalPixels = width * height;
            int[] iterations = new int[totalPixels];
            float[] smooth = new float[totalPixels];

            using var iterBuffer = new ComputeBuffer<int>(_context, ComputeMemoryFlags.WriteOnly, totalPixels);
            using var smoothBuffer = new ComputeBuffer<float>(_context, ComputeMemoryFlags.WriteOnly, totalPixels);

            float minReal = (float)viewPort.MinReal;
            float maxImaginary = (float)viewPort.MaxImaginary;
            float xScale = (float)(viewPort.Width / width);
            float yScale = (float)(viewPort.Height / height);

            _kernel.SetMemoryArgument(0, iterBuffer);
            _kernel.SetMemoryArgument(1, smoothBuffer);
            _kernel.SetValueArgument(2, width);
            _kernel.SetValueArgument(3, height);
            _kernel.SetValueArgument(4, minReal);
            _kernel.SetValueArgument(5, maxImaginary);
            _kernel.SetValueArgument(6, xScale);
            _kernel.SetValueArgument(7, yScale);
            _kernel.SetValueArgument(8, maxIterations);

            long localX = 16, localY = 16;
            long globalX = ((width + localX - 1) / localX) * localX;
            long globalY = ((height + localY - 1) / localY) * localY;

            _queue.Execute(_kernel, null, new long[] { globalX, globalY }, new long[] { localX, localY }, null);
            _queue.Finish();

            // Pobierz wyniki
            GCHandle iterHandle = GCHandle.Alloc(iterations, GCHandleType.Pinned);
            GCHandle smoothHandle = GCHandle.Alloc(smooth, GCHandleType.Pinned);
            try
            {
                _queue.Read(iterBuffer, true, 0, totalPixels, iterHandle.AddrOfPinnedObject(), null);
                _queue.Read(smoothBuffer, true, 0, totalPixels, smoothHandle.AddrOfPinnedObject(), null);
            }
            finally
            {
                iterHandle.Free();
                smoothHandle.Free();
            }

            // Kolorowanie
            Bitmap bitmap = CreateBitmap(width, height, iterations, smooth, maxIterations, colorPalette);

            stopwatch.Stop();

            return new RenderResult
            {
                Bitmap = bitmap,
                RenderTimeMs = stopwatch.ElapsedMilliseconds,
                ThreadsUsed = -1,
                CalculatorName = $"GPU ({_deviceName})",
                ViewPort = viewPort.Clone(),
                ZoomLevel = viewPort.CalculateZoomLevel()
            };
        }

        private Bitmap CreateBitmap(int width, int height, int[] iterations, float[] smooth,
            int maxIterations, ColorPalette colorPalette)
        {
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            BitmapData bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            int stride = bitmapData.Stride;
            byte[] pixels = new byte[height * stride];

            Color[] palette = colorPalette.GeneratePalette(2048);
            int paletteLen = palette.Length;

            Parallel.For(0, height, py =>
            {
                for (int px = 0; px < width; px++)
                {
                    int idx = py * width + px;
                    int iter = iterations[idx];
                    float smoothVal = smooth[idx];

                    int offset = py * stride + px * 4;

                    if (iter >= maxIterations)
                    {
                        pixels[offset] = 0;
                        pixels[offset + 1] = 0;
                        pixels[offset + 2] = 0;
                        pixels[offset + 3] = 255;
                    }
                    else
                    {
                        double colorIdx = smoothVal * 3.0;
                        int ci = ((int)colorIdx) % paletteLen;
                        int ci2 = (ci + 1) % paletteLen;
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

            return bitmap;
        }

        public void Dispose()
        {
            _cpuCalculator?.Dispose();
            _kernel?.Dispose();
            _program?.Dispose();
            _queue?.Dispose();
            _context?.Dispose();
        }
    }
}