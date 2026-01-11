using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ILGPU;
using ILGPU.Runtime;

namespace Mandelbrot.Core.Calculators
{
    /// <summary>
    /// Uproszczona wersja ILGPU - minimalny kernel bez optymalizacji.
    /// Powinno działać na większości systemów.
    /// </summary>
    public class GpuILGPUCalculatorSimple : IMandelbrotCalculator
    {
        private Context _context;
        private Accelerator _accelerator;
        private Action<Index1D, ArrayView<int>, int, int, double, double, double, double, int> _kernel;
        private string _deviceName = "niedostępne";
        private bool _isAvailable = false;

        public string Name => $"GPU ILGPU ({_deviceName})";
        public bool IsAvailable => _isAvailable;

        public GpuILGPUCalculatorSimple()
        {
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                // Prostsza konfiguracja kontekstu
                _context = Context.CreateDefault();

                // Pobierz wszystkie akceleratory
                var accelerators = _context.Devices;

                Console.WriteLine($"Znalezione urządzenia ILGPU: {accelerators.Length}");
                foreach (var device in accelerators)
                {
                    Console.WriteLine($"  - {device.Name} ({device.AcceleratorType})");
                }

                // Wybierz GPU jeśli dostępne, w przeciwnym razie CPU
                Device selectedDevice = null;
                foreach (var device in accelerators)
                {
                    if (device.AcceleratorType == AcceleratorType.Cuda ||
                        device.AcceleratorType == AcceleratorType.OpenCL)
                    {
                        selectedDevice = device;
                        break;
                    }
                }

                // Fallback do CPU
                if (selectedDevice == null)
                {
                    foreach (var device in accelerators)
                    {
                        if (device.AcceleratorType == AcceleratorType.CPU)
                        {
                            selectedDevice = device;
                            break;
                        }
                    }
                }

                if (selectedDevice == null)
                {
                    Console.WriteLine("Nie znaleziono żadnego urządzenia ILGPU");
                    return;
                }

                _accelerator = selectedDevice.CreateAccelerator(_context);
                _deviceName = $"{selectedDevice.Name}";

                Console.WriteLine($"Wybrano: {_deviceName}");

                // Kompilacja PROSTEGO kernela
                _kernel = _accelerator.LoadAutoGroupedStreamKernel<
                    Index1D,           // 1D index zamiast 2D (prostsze)
                    ArrayView<int>,    // Płaska tablica
                    int,               // width
                    int,               // height
                    double,            // minReal
                    double,            // maxImaginary
                    double,            // xScale
                    double,            // yScale
                    int                // maxIterations
                >(SimpleKernel);

                _isAvailable = true;
                Console.WriteLine("ILGPU zainicjalizowane pomyślnie!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ILGPU Init Error: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
                _isAvailable = false;
            }
        }

        /// <summary>
        /// Maksymalnie uproszczony kernel - brak optymalizacji kardioidy,
        /// brak smooth coloring, proste obliczenia.
        /// </summary>
        private static void SimpleKernel(
            Index1D index,
            ArrayView<int> output,
            int width,
            int height,
            double minReal,
            double maxImaginary,
            double xScale,
            double yScale,
            int maxIterations)
        {
            // Konwersja 1D index na 2D współrzędne
            int px = index % width;
            int py = index / width;

            if (px >= width || py >= height)
                return;

            // Współrzędne zespolone
            double x0 = minReal + px * xScale;
            double y0 = maxImaginary - py * yScale;

            // Prosta pętla Mandelbrota
            double x = 0.0;
            double y = 0.0;
            int iteration = 0;

            while (x * x + y * y <= 4.0 && iteration < maxIterations)
            {
                double xtemp = x * x - y * y + x0;
                y = 2.0 * x * y + y0;
                x = xtemp;
                iteration++;
            }

            output[index] = iteration;
        }

        public RenderResult Render(
            int width,
            int height,
            ViewPort viewPort,
            int maxIterations,
            ColorPalette colorPalette)
        {
            if (!_isAvailable || _accelerator == null)
            {
                throw new NotSupportedException("GPU nie jest dostępne");
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            int totalPixels = width * height;

            // Alokacja bufora na GPU
            using var gpuBuffer = _accelerator.Allocate1D<int>(totalPixels);

            // Parametry
            double xScale = viewPort.Width / width;
            double yScale = viewPort.Height / height;

            // Uruchomienie kernela
            _kernel(
                totalPixels,
                gpuBuffer.View,
                width,
                height,
                viewPort.MinReal,
                viewPort.MaxImaginary,
                xScale,
                yScale,
                maxIterations);

            // Synchronizacja
            _accelerator.Synchronize();

            // Pobranie wyników
            int[] iterations = gpuBuffer.GetAsArray1D();

            // Tworzenie bitmapy na CPU
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
            _accelerator?.Dispose();
            _context?.Dispose();
        }
    }
}