using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace Mandelbrot.Core.Calculators
{
    public class CalculatorFactory : IDisposable
    {
        private readonly List<CalculatorInfo> _registry;
        private readonly Dictionary<string, IMandelbrotCalculator> _cache;
        private readonly Dictionary<string, object> _locks;
        private bool _disposed;

        public CalculatorFactory()
        {
            _registry = new List<CalculatorInfo>();
            _cache = new Dictionary<string, IMandelbrotCalculator>();
            _locks = new Dictionary<string, object>();

            Console.WriteLine("\n╔═══════════════════════════════════════╗");
            Console.WriteLine("║         CALCULATOR FACTORY            ║");
            Console.WriteLine("╚═══════════════════════════════════════╝");
            Console.WriteLine($"  SIMD: {(Vector.IsHardwareAccelerated ? $"✓ ({Vector<double>.Count} wide)" : "✗")}");
            Console.WriteLine($"  CPU: {Environment.ProcessorCount} rdzeni\n");

            RegisterCalculators();
            PrintSummary();
        }

        private void RegisterCalculators()
        {
            Register(new CalculatorInfo(
                name: "GPU (OpenCL)",
                description: "🎮 Karta graficzna",
                priority: 1,
                isAvailable: CheckOpenCL,
                factory: () => new GpuCalculator()
            ));

            Register(new CalculatorInfo(
                name: "CPU Parallel",
                description: $"🔄 Wielowątkowy ({Environment.ProcessorCount} rdzeni)",
                priority: 2,
                isAvailable: () => Environment.ProcessorCount > 1,
                factory: () => new CpuParallelCalculator()
            ));

            Register(new CalculatorInfo(
                name: "CPU Single",
                description: "🔹 Jednowątkowy",
                priority: 3,
                isAvailable: () => true,
                factory: () => new CpuSingleCalculator()
            ));
        }

        private void Register(CalculatorInfo info)
        {
            try
            {
                if (info.IsAvailable())
                {
                    _registry.Add(info);
                    _locks[info.Name] = new object();
                    Console.WriteLine($"  ✓ {info.Name}");
                }
                else
                {
                    Console.WriteLine($"  ○ {info.Name} (niedostępny)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ {info.Name}: {ex.Message}");
            }
        }

        private bool CheckOpenCL()
        {
            try
            {
                var platforms = Cloo.ComputePlatform.Platforms;
                return platforms?.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private void PrintSummary()
        {
            Console.WriteLine($"\n  Dostępne: {_registry.Count}/3");
            Console.WriteLine("═══════════════════════════════════════\n");
        }

        public IReadOnlyList<string> AvailableCalculators =>
            _registry.OrderBy(r => r.Priority).Select(r => r.Name).ToList();

        public string GetDescription(string name) =>
            _registry.FirstOrDefault(r => r.Name == name)?.Description ?? "";

        public bool IsLoaded(string name) => _cache.ContainsKey(name);

        public IMandelbrotCalculator GetCalculator(string name)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(CalculatorFactory));

            if (_cache.TryGetValue(name, out var cached))
                return cached;

            return CreateCalculator(name);
        }

        public IMandelbrotCalculator GetDefaultCalculator()
        {
            var first = _registry.OrderBy(r => r.Priority).FirstOrDefault();
            return first != null ? GetCalculator(first.Name)
                : throw new InvalidOperationException("Brak kalkulatorów!");
        }

        private IMandelbrotCalculator CreateCalculator(string name)
        {
            var info = _registry.FirstOrDefault(r => r.Name == name)
                       ?? _registry.FirstOrDefault();

            if (info == null)
                throw new InvalidOperationException("Brak kalkulatorów!");

            lock (_locks[info.Name])
            {
                if (_cache.TryGetValue(info.Name, out var cached))
                    return cached;

                Console.WriteLine($"  🔨 Tworzę: {info.Name}...");
                var sw = Stopwatch.StartNew();

                var calc = info.Factory();

                sw.Stop();
                Console.WriteLine($"  ✅ Gotowe ({sw.ElapsedMilliseconds}ms)\n");

                _cache[info.Name] = calc;
                return calc;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var calc in _cache.Values)
            {
                try { calc.Dispose(); }
                catch { }
            }
            _cache.Clear();
        }
    }
}