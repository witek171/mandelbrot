using System.Drawing.Drawing2D;

namespace Mandelbrot.WinForms;

public class ZoomEventArgs : EventArgs
{
	public ZoomEventArgs(int startX, int startY, int endX, int endY)
	{
		StartX = startX;
		StartY = startY;
		EndX = endX;
		EndY = endY;
	}

	public int StartX { get; }
	public int StartY { get; }
	public int EndX { get; }
	public int EndY { get; }
}

public class MouseWheelZoomEventArgs : EventArgs
{
	public MouseWheelZoomEventArgs(int x, int y, bool zoomIn)
	{
		X = x;
		Y = y;
		ZoomIn = zoomIn;
	}

	public int X { get; }
	public int Y { get; }
	public bool ZoomIn { get; }
}

public class MandelbrotPanel : Panel
{
	private Bitmap _image;
	private bool _isSelecting;
	private Point _selectionEnd;
	private Point _selectionStart;

	public MandelbrotPanel()
	{
		DoubleBuffered = true;
		SetStyle(ControlStyles.AllPaintingInWmPaint |
				ControlStyles.UserPaint |
				ControlStyles.OptimizedDoubleBuffer, true);
	}

	public event EventHandler<ZoomEventArgs> ZoomRequested;
	public event EventHandler<MouseWheelZoomEventArgs> MouseWheelZoom;
	public event EventHandler RightClickBack;
	public event MouseEventHandler MouseMoved;

	public void SetImage(Bitmap bitmap)
	{
		_image = bitmap;
		Invalidate();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		if (_image != null) e.Graphics.DrawImage(_image, 0, 0);

		if (_isSelecting)
		{
			Rectangle rect = GetSelectionRectangle();
			using Pen pen = new(Color.White, 2) { DashStyle = DashStyle.Dash };
			using SolidBrush brush = new(Color.FromArgb(50, 255, 255, 255));
			e.Graphics.FillRectangle(brush, rect);
			e.Graphics.DrawRectangle(pen, rect);
		}
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);

		if (e.Button == MouseButtons.Left)
		{
			_isSelecting = true;
			_selectionStart = e.Location;
			_selectionEnd = e.Location;
		}
		else if (e.Button == MouseButtons.Right)
		{
			RightClickBack?.Invoke(this, EventArgs.Empty);
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		MouseMoved?.Invoke(this, e);

		if (_isSelecting)
		{
			_selectionEnd = e.Location;
			Invalidate();
		}
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);

		if (_isSelecting && e.Button == MouseButtons.Left)
		{
			_isSelecting = false;
			Invalidate();

			Rectangle rect = GetSelectionRectangle();
			if (rect.Width > 10 && rect.Height > 10)
				ZoomRequested?.Invoke(this, new ZoomEventArgs(
					rect.X, rect.Y,
					rect.X + rect.Width,
					rect.Y + rect.Height));
		}
	}

	protected override void OnMouseWheel(MouseEventArgs e)
	{
		base.OnMouseWheel(e);
		MouseWheelZoom?.Invoke(this, new MouseWheelZoomEventArgs(e.X, e.Y, e.Delta > 0));
	}

	private Rectangle GetSelectionRectangle()
	{
		int x = Math.Min(_selectionStart.X, _selectionEnd.X);
		int y = Math.Min(_selectionStart.Y, _selectionEnd.Y);
		int w = Math.Abs(_selectionEnd.X - _selectionStart.X);
		int h = Math.Abs(_selectionEnd.Y - _selectionStart.Y);
		return new Rectangle(x, y, w, h);
	}
}