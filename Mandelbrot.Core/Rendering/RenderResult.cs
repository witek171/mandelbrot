using System;
using System.Drawing;

namespace Mandelbrot.Core.Rendering
{
	public class RenderResult
	{
		public Bitmap Bitmap { get; }
		public TimeSpan RenderTime { get; }
		public double ZoomLevel { get; }

		public string FormattedTime
		{
			get
			{
				double ms = RenderTime.TotalMilliseconds;
				if (ms < 1) return $"{RenderTime.TotalMicroseconds:F0}μs";
				if (ms < 1000) return $"{ms:F0}ms";
				return $"{RenderTime.TotalSeconds:F2}s";
			}
		}

		public string FormattedZoom
		{
			get
			{
				if (ZoomLevel >= 1e12) return $"{ZoomLevel / 1e12:F1}T×";
				if (ZoomLevel >= 1e9) return $"{ZoomLevel / 1e9:F1}G×";
				if (ZoomLevel >= 1e6) return $"{ZoomLevel / 1e6:F1}M×";
				if (ZoomLevel >= 1e3) return $"{ZoomLevel / 1e3:F1}K×";
				return $"{ZoomLevel:F1}×";
			}
		}

		public RenderResult(Bitmap bitmap, TimeSpan renderTime, double zoomLevel)
		{
			Bitmap = bitmap;
			RenderTime = renderTime;
			ZoomLevel = zoomLevel;
		}
	}
}