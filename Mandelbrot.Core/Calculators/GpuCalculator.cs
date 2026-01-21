using System.Diagnostics;
using Cloo;
using Mandelbrot.Core.Rendering;

namespace Mandelbrot.Core.Calculators;

public sealed class GpuCalculator : IMandelbrotCalculator
{
	private const string KernelTemplate = @"
            #EXTENSION#

            __kernel void mandelbrot(
                __global int* output,
                const #TYPE# xMin, const #TYPE# yMax,
                const #TYPE# xScale, const #TYPE# yScale,
                const int width, const int maxIter)
            {
                int px = get_global_id(0);
                int py = get_global_id(1);
                
                if (px >= width || py >= get_global_size(1)) return;

                #TYPE# x0 = xMin + px * xScale;
                #TYPE# y0 = yMax - py * yScale;

                // Optymalizacja: sprawdzenie kardioidy i bańki
                #TYPE# y2 = y0 * y0;
                #TYPE# q = (x0 - 0.25#S#) * (x0 - 0.25#S#) + y2;
                
                if (q * (q + (x0 - 0.25#S#)) <= 0.25#S# * y2) { 
                    output[py * width + px] = maxIter; 
                    return; 
                }

                #TYPE# x = 0.0#S#, y = 0.0#S#;
                #TYPE# x2 = 0.0#S#, y2Loop = 0.0#S#;
                int iter = 0;

                while (x2 + y2Loop <= 4.0#S# && iter < maxIter) {
                    y = 2.0#S# * x * y + y0;
                    x = x2 - y2Loop + x0;
                    x2 = x * x;
                    y2Loop = y * y;
                    iter++;
                }
                
                if (py * width + px < width * get_global_size(1))
                    output[py * width + px] = iter;
            }";

	private readonly long[] _localWorkSize = { 16, 16 };

	private ComputeContext _context;
	private ComputeKernel _kernel;
	private ComputeProgram _program;
	private ComputeCommandQueue _queue;

	public GpuCalculator()
	{
		Initialize();
	}

	public bool UsesDouble { get; private set; }
	public string Name => "GPU (OpenCL)";

	public IterationData CalculateIterations(int width, int height, ViewPort viewPort, int maxIterations)
	{
		Stopwatch sw = Stopwatch.StartNew();
		int[] iterations = new int[width * height];

		double xScale = viewPort.Width / width;
		double yScale = viewPort.Height / height;

		using (ComputeBuffer<int> buffer = new(_context, ComputeMemoryFlags.WriteOnly, iterations.Length))
		{
			_kernel.SetMemoryArgument(0, buffer);

			if (UsesDouble)
			{
				_kernel.SetValueArgument(1, viewPort.MinReal);
				_kernel.SetValueArgument(2, viewPort.MaxImaginary);
				_kernel.SetValueArgument(3, xScale);
				_kernel.SetValueArgument(4, yScale);
			}
			else
			{
				_kernel.SetValueArgument(1, (float)viewPort.MinReal);
				_kernel.SetValueArgument(2, (float)viewPort.MaxImaginary);
				_kernel.SetValueArgument(3, (float)xScale);
				_kernel.SetValueArgument(4, (float)yScale);
			}

			_kernel.SetValueArgument(5, width);
			_kernel.SetValueArgument(6, maxIterations);

			long gW = (width + 15) / 16 * 16;
			long gH = (height + 15) / 16 * 16;

			_queue.Execute(_kernel, null, new[] { gW, gH }, _localWorkSize, null);
			_queue.ReadFromBuffer(buffer, ref iterations, true, null);
		}

		sw.Stop();
		return new IterationData(iterations, width, height, maxIterations, viewPort, sw.Elapsed);
	}

	public void Dispose()
	{
		_kernel.Dispose();
		_program.Dispose();
		_queue.Dispose();
		_context.Dispose();
	}

	private void Initialize()
	{
		try
		{
			ComputeDevice? device = ComputePlatform.Platforms
				.SelectMany(p => p.Devices)
				.OrderByDescending(d => d.Type == ComputeDeviceTypes.Gpu)
				.FirstOrDefault();

			if (device == null)
				return;

			UsesDouble = device.Extensions.Contains("cl_khr_fp64") ||
						device.Extensions.Contains("cl_amd_fp64");

			string source = KernelTemplate;

			if (UsesDouble)
			{
				source = source.Replace("#EXTENSION#", "#pragma OPENCL EXTENSION cl_khr_fp64 : enable");
				source = source.Replace("#TYPE#", "double");
				source = source.Replace("#S#", "");
			}
			else
			{
				source = source.Replace("#EXTENSION#", "");
				source = source.Replace("#TYPE#", "float");
				source = source.Replace("#S#", "f");
			}

			ComputeContextPropertyList props = new(device.Platform);
			_context = new ComputeContext(new[] { device }, props, null, IntPtr.Zero);
			_queue = new ComputeCommandQueue(_context, device, ComputeCommandQueueFlags.None);
			_program = new ComputeProgram(_context, source);

			try
			{
				_program.Build(null, null, null, IntPtr.Zero);
			}
			catch (Exception ex)
			{
				string log = _program.GetBuildLog(device);
				throw new InvalidOperationException($"Błąd kompilacji kernela OpenCL:\n{log}", ex);
			}

			_kernel = _program.CreateKernel("mandelbrot");
		}
		catch
		{
			// ignored
		}
	}
}