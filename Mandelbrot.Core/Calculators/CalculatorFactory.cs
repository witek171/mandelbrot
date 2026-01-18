using System;
using System.Collections.Generic;
using System.Linq;

namespace Mandelbrot.Core.Calculators
{
	public class CalculatorFactory : IDisposable
	{
		private readonly Dictionary<string, IMandelbrotCalculator> _calculators;
		private readonly List<string> _availableNames;

		public CalculatorFactory()
		{
			_calculators = new Dictionary<string, IMandelbrotCalculator>();
			_availableNames = new List<string>();

			Console.WriteLine("╔════════════════════════════════════════════╗");
			Console.WriteLine("║       Inicjalizacja kalkulatorów           ║");
			Console.WriteLine("╚════════════════════════════════════════════╝\n");

			// Główny - hybrydowy GPU/CPU (rekomendowany)
			RegisterSafe(() => new GpuHybridCalculator());

			// CPU alternatywy
			RegisterSafe(() => new CpuFastCalculator());
			RegisterSafe(() => new CpuParallelCalculator());
			RegisterSafe(() => new CpuSingleThreadCalculator());

			//RegisterSafe(() => new GpuILGPUCalculatorSimple());
			RegisterSafe(() => new GpuClooCalculator());

			Console.WriteLine("\n════════════════════════════════════════════");
			Console.WriteLine($"  Dostępne: {_availableNames.Count} kalkulatorów");
			foreach (var name in _availableNames)
			{
				Console.WriteLine($"    • {name}");
			}

			Console.WriteLine("════════════════════════════════════════════\n");
		}

		private void RegisterSafe(Func<IMandelbrotCalculator> factory)
		{
			try
			{
				var calc = factory();
				if (calc.IsAvailable && !_calculators.ContainsKey(calc.Name))
				{
					_calculators[calc.Name] = calc;
					_availableNames.Add(calc.Name);
					Console.WriteLine($"  ✓ {calc.Name}");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"  ✗ Błąd: {ex.Message}");
			}
		}

		public IReadOnlyList<string> AvailableCalculators => _availableNames;

		public IMandelbrotCalculator GetCalculator(string name)
		{
			if (_calculators.TryGetValue(name, out var calc))
				return calc;
			return _calculators.Values.First();
		}

		public IMandelbrotCalculator GetFastestCalculator()
		{
			// Hybrid jest najlepszy - automatycznie przełącza GPU/CPU
			var hybrid = _calculators.Values.FirstOrDefault(c => c.Name.Contains("Hybrid"));
			if (hybrid != null) return hybrid;

			var fast = _calculators.Values.FirstOrDefault(c => c.Name.Contains("Fast"));
			if (fast != null) return fast;

			return _calculators.Values.First();
		}

		public void Dispose()
		{
			foreach (var calc in _calculators.Values)
			{
				try
				{
					calc.Dispose();
				}
				catch
				{
				}
			}
		}
	}
}