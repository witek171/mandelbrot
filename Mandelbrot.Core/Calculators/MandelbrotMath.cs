using System.Runtime.CompilerServices;

namespace Mandelbrot.Core.Calculators;

public static class MandelbrotMath
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int CalculatePoint(double x0, double y0, int maxIterations)
	{
		double xMinus = x0 - 0.25;
		double y2 = y0 * y0;
		double q = xMinus * xMinus + y2;
		if (q * (q + xMinus) <= 0.25 * y2)
			return maxIterations;

		double xPlus = x0 + 1.0;
		if (xPlus * xPlus + y2 <= 0.0625)
			return maxIterations;

		double x = 0, y = 0;
		double x2 = 0, y2Loop = 0;
		int iter = 0;

		while (x2 + y2Loop <= 4.0 && iter < maxIterations)
		{
			y = 2.0 * x * y + y0;
			x = x2 - y2Loop + x0;
			x2 = x * x;
			y2Loop = y * y;
			iter++;
		}

		return iter;
	}
}