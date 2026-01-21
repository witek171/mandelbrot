namespace Mandelbrot.Core.Calculators;

public class CalculatorFactory : IDisposable
{
	private readonly Dictionary<string, Func<IMandelbrotCalculator>> _factories = new();
	private readonly Dictionary<string, IMandelbrotCalculator> _cache = new();
	private readonly List<string> _available = [];
	private bool _disposed;

	public CalculatorFactory()
	{
		TryRegister("GPU (OpenCL)", CheckOpenCL, () => new GpuCalculator());
		TryRegister("CPU Parallel", () => Environment.ProcessorCount > 1, () => new CpuParallelCalculator());
		TryRegister("CPU Single", () => true, () => new CpuSingleCalculator());
	}

	private void TryRegister(string name, Func<bool> isAvailable, Func<IMandelbrotCalculator> factory)
	{
		try
		{
			if (!isAvailable())
				return;

			_factories[name] = factory;
			_available.Add(name);
		}
		catch
		{
			// ignored
		}
	}

	private static bool CheckOpenCL()
	{
		try
		{
			return Cloo.ComputePlatform.Platforms?.Count > 0;
		}
		catch
		{
			return false;
		}
	}

	public IReadOnlyList<string> AvailableCalculators => _available;

	public IMandelbrotCalculator GetCalculator(string? name = null)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);

		string key = name ?? _available.FirstOrDefault()
			?? throw new InvalidOperationException("Brak dostępnych kalkulatorów!");

		if (!_factories.ContainsKey(key))
			throw new ArgumentException($"Nieznany kalkulator: {key}");

		if (!_cache.TryGetValue(key, out IMandelbrotCalculator? calculator))
		{
			calculator = _factories[key]();
			_cache[key] = calculator;
		}

		return calculator;
	}

	public void Dispose()
	{
		if (_disposed) return;
		_disposed = true;

		foreach (IMandelbrotCalculator calc in _cache.Values)
		{
			try
			{
				calc.Dispose();
			}
			catch
			{
				// ignored
			}
		}

		_cache.Clear();
		_factories.Clear();
	}
}