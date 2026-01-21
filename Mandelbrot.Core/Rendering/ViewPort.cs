namespace Mandelbrot.Core.Rendering;

public readonly struct ViewPort
{
	public double MinReal { get; }
	public double MaxReal { get; }
	public double MinImaginary { get; }
	public double MaxImaginary { get; }

	public double Width => MaxReal - MinReal;
	public double Height => MaxImaginary - MinImaginary;

	public static ViewPort Default => new(-2.5, 1.0, -1.25, 1.25);

	public ViewPort(double minReal, double maxReal, double minImag, double maxImag)
	{
		MinReal = minReal;
		MaxReal = maxReal;
		MinImaginary = minImag;
		MaxImaginary = maxImag;
	}

	public double CalculateZoomLevel()
	{
		const double defaultWidth = 3.5;
		return defaultWidth / Width;
	}

	public ViewPort Zoom(int startX, int startY, int endX, int endY,
		int screenWidth, int screenHeight, bool maintainAspect)
	{
		double xScale = Width / screenWidth;
		double yScale = Height / screenHeight;

		double newMinReal = MinReal + Math.Min(startX, endX) * xScale;
		double newMaxReal = MinReal + Math.Max(startX, endX) * xScale;
		double newMaxImag = MaxImaginary - Math.Min(startY, endY) * yScale;
		double newMinImag = MaxImaginary - Math.Max(startY, endY) * yScale;

		if (maintainAspect)
		{
			double newWidth = newMaxReal - newMinReal;
			double newHeight = newMaxImag - newMinImag;
			double screenAspect = (double)screenWidth / screenHeight;
			double selectionAspect = newWidth / newHeight;

			if (selectionAspect > screenAspect)
			{
				double adjustedHeight = newWidth / screenAspect;
				double centerImag = (newMinImag + newMaxImag) / 2;
				newMinImag = centerImag - adjustedHeight / 2;
				newMaxImag = centerImag + adjustedHeight / 2;
			}
			else
			{
				double adjustedWidth = newHeight * screenAspect;
				double centerReal = (newMinReal + newMaxReal) / 2;
				newMinReal = centerReal - adjustedWidth / 2;
				newMaxReal = centerReal + adjustedWidth / 2;
			}
		}

		return new ViewPort(newMinReal, newMaxReal, newMinImag, newMaxImag);
	}

	public ViewPort ZoomAtPoint(int x, int y, int screenWidth, int screenHeight, double factor)
	{
		double xScale = Width / screenWidth;
		double yScale = Height / screenHeight;

		double centerReal = MinReal + x * xScale;
		double centerImag = MaxImaginary - y * yScale;

		double newWidth = Width / factor;
		double newHeight = Height / factor;

		return new ViewPort(
			centerReal - newWidth / 2,
			centerReal + newWidth / 2,
			centerImag - newHeight / 2,
			centerImag + newHeight / 2
		);
	}

	public string GetCacheKey(int width, int height, int maxIter)
		=> $"{MinReal:G17}_{MaxReal:G17}_{MinImaginary:G17}_{MaxImaginary:G17}_{width}_{height}_{maxIter}";
}