// using BenchmarkDotNet.Attributes;
// using BenchmarkDotNet.Running;
// using Mandelbrot.Core;
// using System;
//
// namespace Mandelbrot.Benchmarks
// {
// 	/// <summary>
// 	/// Klasa zawierająca benchmarki do porównania wydajności różnych implementacji.
// 	/// Uruchom przez: dotnet run -c Release
// 	/// </summary>
// 	[MemoryDiagnoser]
// 	[RankColumn]
// 	public class MandelbrotBenchmarks
// 	{
// 		private MandelbrotCalculator _calculator;
// 		private ViewPort _viewPort;
//
// 		[Params(256, 512, 1024)] public int ImageSize { get; set; }
//
// 		[Params(100, 500, 1000)] public int MaxIterations { get; set; }
//
// 		[GlobalSetup]
// 		public void Setup()
// 		{
// 			_calculator = new MandelbrotCalculator();
// 			_viewPort = ViewPort.Default;
// 		}
//
// 		/// <summary>
// 		/// Benchmark wersji jednowątkowej.
// 		/// </summary>
// 		[Benchmark(Baseline = true)]
// 		public void SingleThreaded()
// 		{
// 			var result = _calculator.RenderSingleThreaded(ImageSize, ImageSize, _viewPort, MaxIterations);
// 			result.Bitmap.Dispose();
// 		}
//
// 		/// <summary>
// 		/// Benchmark wersji wielowątkowej.
// 		/// </summary>
// 		[Benchmark]
// 		public void MultiThreaded()
// 		{
// 			var result = _calculator.RenderMultiThreaded(ImageSize, ImageSize, _viewPort, MaxIterations);
// 			result.Bitmap.Dispose();
// 		}
//
// 		/// <summary>
// 		/// Benchmark porównujący implementację double vs Complex.
// 		/// </summary>
// 		[Benchmark]
// 		public int IterationsWithDouble()
// 		{
// 			int total = 0;
// 			double step = 0.01;
// 			for (double re = -2.0; re < 1.0; re += step)
// 			{
// 				for (double im = -1.5; im < 1.5; im += step)
// 				{
// 					// Używa wersji z double (zoptymalizowana)
// 					total += CalculateWithDouble(re, im, MaxIterations);
// 				}
// 			}
//
// 			return total;
// 		}
//
// 		[Benchmark]
// 		public int IterationsWithComplex()
// 		{
// 			int total = 0;
// 			double step = 0.01;
// 			for (double re = -2.0; re < 1.0; re += step)
// 			{
// 				for (double im = -1.5; im < 1.5; im += step)
// 				{
// 					// Używa wersji z System.Numerics.Complex
// 					total += _calculator.CalculateIterationsWithComplex(re, im, MaxIterations);
// 				}
// 			}
//
// 			return total;
// 		}
//
// 		/// <summary>
// 		/// Zoptymalizowana implementacja używająca double.
// 		/// </summary>
// 		private int CalculateWithDouble(double x0, double y0, int maxIterations)
// 		{
// 			double x = 0.0, y = 0.0;
// 			double x2 = 0.0, y2 = 0.0;
// 			int iteration = 0;
//
// 			while (x2 + y2 <= 4.0 && iteration < maxIterations)
// 			{
// 				y = 2.0 * x * y + y0;
// 				x = x2 - y2 + x0;
// 				x2 = x * x;
// 				y2 = y * y;
// 				iteration++;
// 			}
//
// 			return iteration;
// 		}
// 	}
//
// 	/// <summary>
// 	/// Program uruchamiający benchmarki.
// 	/// </summary>
// 	public class Program
// 	{
// 		public static void Main(string[] args)
// 		{
// 			Console.WriteLine("=== Benchmarki Fraktala Mandelbrota ===\n");
// 			Console.WriteLine("Uruchamianie benchmarków... (może to potrwać kilka minut)\n");
//
// 			var summary = BenchmarkRunner.Run<MandelbrotBenchmarks>();
//
// 			Console.WriteLine("\n=== Podsumowanie ===");
// 			Console.WriteLine("Wyniki zostały zapisane w katalogu BenchmarkDotNet.Artifacts");
// 		}
// 	}
// }