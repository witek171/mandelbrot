using System.Drawing;
using System.Runtime.CompilerServices;

namespace Mandelbrot.Core.Rendering;

public class ColorPalette
{
	public enum PaletteType
	{
		Rainbow,
		Fire,
		Ocean,
		Grayscale,
		Electric,
		Sunset,
		Forest,
		Neon
	}

	private const int CacheSize = 4096;

	private readonly int[][] _cache;

	public ColorPalette()
	{
		_cache = new int[8][];
		for (int i = 0; i < 8; i++)
			_cache[i] = GeneratePalette((PaletteType)i, CacheSize);
	}

	public PaletteType CurrentPalette { get; set; } = PaletteType.Rainbow;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetColorArgb(int iteration, int maxIterations)
	{
		if (iteration >= maxIterations)
			return unchecked((int)0xFF000000);

		int index = iteration * (CacheSize - 1) / maxIterations;
		return _cache[(int)CurrentPalette][index];
	}

	private static int[] GeneratePalette(PaletteType type, int size)
	{
		int[] palette = new int[size];

		for (int i = 0; i < size; i++)
		{
			double t = (double)i / size;
			Color c = type switch
			{
				PaletteType.Rainbow => Rainbow(t),
				PaletteType.Fire => Fire(t),
				PaletteType.Ocean => Ocean(t),
				PaletteType.Grayscale => Grayscale(t),
				PaletteType.Electric => Electric(t),
				PaletteType.Sunset => Sunset(t),
				PaletteType.Forest => Forest(t),
				PaletteType.Neon => Neon(t),
				_ => Rainbow(t)
			};
			palette[i] = c.ToArgb();
		}

		return palette;
	}

	private static Color Rainbow(double t)
		=> FromHsv(t * 360, 1, 1);

	private static Color Fire(double t)
	{
		int r = Math.Min(255, (int)(t * 3 * 255));
		int g = Math.Min(255, (int)(Math.Max(0, t * 3 - 1) * 255));
		int b = Math.Min(255, (int)(Math.Max(0, t * 3 - 2) * 255));
		return Color.FromArgb(255, r, g, b);
	}

	private static Color Ocean(double t)
	{
		int r = (int)(t * 50);
		int g = (int)(50 + t * 150);
		int b = (int)(150 + t * 105);
		return Color.FromArgb(255, r, Math.Min(255, g), Math.Min(255, b));
	}

	private static Color Grayscale(double t)
	{
		int v = (int)(t * 255);
		return Color.FromArgb(255, v, v, v);
	}

	private static Color Electric(double t)
		=> FromHsv(240 + t * 60, 1, 0.5 + t * 0.5);

	private static Color Sunset(double t)
	{
		int r = (int)(255 * (0.5 + 0.5 * Math.Sin(t * Math.PI)));
		int g = (int)(128 * t);
		int b = (int)(255 * (1 - t) * 0.5);
		return Color.FromArgb(255, r, g, b);
	}

	private static Color Forest(double t)
	{
		int r = (int)(50 * t);
		int g = (int)(100 + 155 * t);
		int b = (int)(50 * t);
		return Color.FromArgb(255, r, g, b);
	}

	private static Color Neon(double t)
		=> FromHsv((t * 120 + 280) % 360, 1, 1);

	private static Color FromHsv(double h, double s, double v)
	{
		h = h % 360;
		int hi = (int)(h / 60) % 6;
		double f = h / 60 - hi;
		double p = v * (1 - s);
		double q = v * (1 - f * s);
		double t = v * (1 - (1 - f) * s);

		double r, g, b;
		switch (hi)
		{
			case 0:
				r = v;
				g = t;
				b = p;
				break;
			case 1:
				r = q;
				g = v;
				b = p;
				break;
			case 2:
				r = p;
				g = v;
				b = t;
				break;
			case 3:
				r = p;
				g = q;
				b = v;
				break;
			case 4:
				r = t;
				g = p;
				b = v;
				break;
			default:
				r = v;
				g = p;
				b = q;
				break;
		}

		return Color.FromArgb(255, (int)(r * 255), (int)(g * 255), (int)(b * 255));
	}
}