using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Cloo;

namespace Mandelbrot.Core.Calculators
{
    /// <summary>
    /// Implementacja GPU używając biblioteki Cloo (OpenCL).
    /// Działa na NVIDIA, AMD i Intel GPU.
    /// </summary>
    public class GpuClooCalculator : IMandelbrotCalculator
    {
        private ComputePlatform _platform;
        private ComputeContext _context;
        private ComputeCommandQueue _queue;
        private ComputeProgram _program;
        private ComputeKernel _kernel;
        private string _deviceName = "niedostępne";
        private bool _isAvailable = false;

        // Kod OpenCL - prosty i kompatybilny
        private const string KernelSource = @"
            __kernel void mandelbrot(
                __global int* output,
                const int width,
                const int height,
                const float minReal,        // ZMIANA: float
                const float maxImaginary,   // ZMIANA: float
                const float xScale,         // ZMIANA: float
                const float yScale,         // ZMIANA: float
                const int maxIterations)
            {
                int px = get_global_id(0);
                int py = get_global_id(1);
        
                if (px >= width || py >= height)
                    return;
        
                // ZMIANA: rzutowanie na (float) i literały f
                float x0 = minReal + (float)px * xScale;
                float y0 = maxImaginary - (float)py * yScale;
        
                float x = 0.0f;
                float y = 0.0f;
                float x2 = 0.0f;
                float y2 = 0.0f;
                int iteration = 0;
        
                while (x2 + y2 <= 4.0f && iteration < maxIterations)
                {
                    y = 2.0f * x * y + y0;
                    x = x2 - y2 + x0;
                    x2 = x * x;
                    y2 = y * y;
                    iteration++;
                }
        
                output[py * width + px] = iteration;
            }
        ";

        public string Name => $"GPU OpenCL ({_deviceName})";
        public bool IsAvailable => _isAvailable;

        public GpuClooCalculator()
        {
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                // Znajdź platformę OpenCL
                var platforms = ComputePlatform.Platforms;

                Console.WriteLine($"Znalezione platformy OpenCL: {platforms.Count}");

                if (platforms.Count == 0)
                {
                    Console.WriteLine("Brak platform OpenCL!");
                    return;
                }

                // Szukaj GPU
                ComputeDevice selectedDevice = null;

                foreach (var platform in platforms)
                {
                    Console.WriteLine($"Platforma: {platform.Name}");

                    var devices = platform.Devices
                        .Where(d => d.Type == ComputeDeviceTypes.Gpu)
                        .ToList();

                    foreach (var device in devices)
                    {
                        Console.WriteLine($"  GPU: {device.Name}");

                        // Sprawdź czy obsługuje double
                        if (device.Extensions.Contains("cl_khr_fp64"))
                        {
                            selectedDevice = device;
                            _platform = platform;
                            break;
                        }
                        else
                        {
                            Console.WriteLine($"    (brak wsparcia double - pomijam)");
                        }
                    }

                    if (selectedDevice != null) break;
                }

                // Fallback - GPU bez sprawdzania double (użyjemy float)
                if (selectedDevice == null)
                {
                    foreach (var platform in platforms)
                    {
                        var devices = platform.Devices
                            .Where(d => d.Type == ComputeDeviceTypes.Gpu)
                            .ToList();

                        if (devices.Any())
                        {
                            selectedDevice = devices.First();
                            _platform = platform;
                            break;
                        }
                    }
                }

                if (selectedDevice == null)
                {
                    Console.WriteLine("Nie znaleziono GPU z OpenCL");
                    return;
                }

                _deviceName = selectedDevice.Name;
                Console.WriteLine($"Wybrano GPU: {_deviceName}");

                // Utwórz kontekst
                var properties = new ComputeContextPropertyList(_platform);
                _context = new ComputeContext(
                    new[] { selectedDevice },
                    properties,
                    null,
                    IntPtr.Zero);

                // Utwórz kolejkę poleceń
                _queue = new ComputeCommandQueue(
                    _context,
                    selectedDevice,
                    ComputeCommandQueueFlags.None);

                // Kompiluj program
                _program = new ComputeProgram(_context, KernelSource);

                try
                {
                    _program.Build(new[] { selectedDevice }, "-cl-fast-relaxed-math", null, IntPtr.Zero);
                }
                catch (ComputeException)
                {
                    string buildLog = _program.GetBuildLog(selectedDevice);
                    Console.WriteLine($"OpenCL Build Error:\n{buildLog}");
                    return;
                }

                // Utwórz kernel
                _kernel = _program.CreateKernel("mandelbrot");

                _isAvailable = true;
                Console.WriteLine("OpenCL zainicjalizowane pomyślnie!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OpenCL Init Error: {ex.Message}");
                _isAvailable = false;
            }
        }

        public RenderResult Render(
            int width,
            int height,
            ViewPort viewPort,
            int maxIterations,
            ColorPalette colorPalette)
        {
            if (!_isAvailable)
            {
                throw new NotSupportedException("OpenCL nie jest dostępne");
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            int totalPixels = width * height;
            int[] iterations = new int[totalPixels];

            // Bufor na GPU
            var outputBuffer = new ComputeBuffer<int>(
                _context,
                ComputeMemoryFlags.WriteOnly,
                totalPixels);

            // Parametry
            double xScale = viewPort.Width / width;
            double yScale = viewPort.Height / height;

            // Ustaw argumenty kernela
            _kernel.SetMemoryArgument(0, outputBuffer);
            _kernel.SetValueArgument(1, width);
            _kernel.SetValueArgument(2, height);
            _kernel.SetValueArgument(3, (float)viewPort.MinReal);
            _kernel.SetValueArgument(4, (float)viewPort.MaxImaginary);
            _kernel.SetValueArgument(5, (float)xScale);
            _kernel.SetValueArgument(6, (float)yScale);
            _kernel.SetValueArgument(7, maxIterations);

            // Uruchom kernel
            _queue.Execute(
                _kernel,
                null,
                new long[] { width, height },
                null,
                null);

            // Poczekaj na zakończenie
            _queue.Finish();

            // Pobierz wyniki
            GCHandle handle = GCHandle.Alloc(iterations, GCHandleType.Pinned);
            try
            {
                _queue.Read(outputBuffer, true, 0, totalPixels, handle.AddrOfPinnedObject(), null);
            }
            finally
            {
                handle.Free();
            }

            outputBuffer.Dispose();

            // Tworzenie bitmapy
            Bitmap bitmap = CreateBitmap(width, height, iterations, maxIterations, colorPalette);

            stopwatch.Stop();

            return new RenderResult
            {
                Bitmap = bitmap,
                RenderTimeMs = stopwatch.ElapsedMilliseconds,
                ThreadsUsed = -1,
                CalculatorName = Name,
                ViewPort = viewPort.Clone(),
                ZoomLevel = viewPort.CalculateZoomLevel()
            };
        }

        private Bitmap CreateBitmap(int width, int height, int[] iterations,
            int maxIterations, ColorPalette colorPalette)
        {
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            BitmapData bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            int stride = bitmapData.Stride;
            byte[] pixels = new byte[height * stride];

            Parallel.For(0, height, py =>
            {
                for (int px = 0; px < width; px++)
                {
                    int iter = iterations[py * width + px];
                    Color color = iter >= maxIterations
                        ? Color.Black
                        : colorPalette.GetSmoothColor(iter, maxIterations);

                    int offset = py * stride + px * 4;
                    pixels[offset] = color.B;
                    pixels[offset + 1] = color.G;
                    pixels[offset + 2] = color.R;
                    pixels[offset + 3] = 255;
                }
            });

            Marshal.Copy(pixels, 0, bitmapData.Scan0, pixels.Length);
            bitmap.UnlockBits(bitmapData);

            return bitmap;
        }

        public void Dispose()
        {
            _kernel?.Dispose();
            _program?.Dispose();
            _queue?.Dispose();
            _context?.Dispose();
        }
    }
}