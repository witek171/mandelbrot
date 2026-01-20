using System.Drawing;

namespace Mandelbrot.Core
{
	public class RenderResult
	{
		public Bitmap Bitmap { get; set; }
		public long RenderTimeMs { get; set; }
		public int ThreadsUsed { get; set; }
		public string CalculatorName { get; set; }
		public ViewPort ViewPort { get; set; }
		public double ZoomLevel { get; set; }

		public string FormattedTime
		{
			get
			{
				if (RenderTimeMs < 1000)
					return $"{RenderTimeMs} ms";
				else
					return $"{RenderTimeMs / 1000.0:F2} s";
			}
		}

		public string FormattedZoom
		{
			get
			{
				if (ZoomLevel < 1000)
					return $"{ZoomLevel:F1}x";
				else if (ZoomLevel < 1_000_000)
					return $"{ZoomLevel / 1000:F1}K x";
				else if (ZoomLevel < 1_000_000_000)
					return $"{ZoomLevel / 1_000_000:F1}M x";
				else if (ZoomLevel < 1_000_000_000_000)
					return $"{ZoomLevel / 1_000_000_000:F1}G x";
				else
					return $"{ZoomLevel / 1_000_000_000_000:F1}T x";
			}
		}
	}
}