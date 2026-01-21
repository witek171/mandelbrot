using System;

namespace Mandelbrot.Core.Calculators
{
	public class CalculatorInfo
	{
		public string Name { get; }
		public string Description { get; }
		public int Priority { get; }
		public Func<bool> IsAvailable { get; }
		public Func<IMandelbrotCalculator> Factory { get; }

		public CalculatorInfo(
			string name,
			string description,
			int priority,
			Func<bool> isAvailable,
			Func<IMandelbrotCalculator> factory)
		{
			Name = name;
			Description = description;
			Priority = priority;
			IsAvailable = isAvailable;
			Factory = factory;
		}
	}
}