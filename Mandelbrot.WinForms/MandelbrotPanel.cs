using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Mandelbrot.WinForms
{
    /// <summary>
    /// Argumenty zdarzenia zoomu przez zaznaczenie.
    /// </summary>
    public class ZoomEventArgs : EventArgs
    {
        public int StartX { get; set; }
        public int StartY { get; set; }
        public int EndX { get; set; }
        public int EndY { get; set; }
    }

    /// <summary>
    /// Argumenty zdarzenia zoomu kółkiem myszy.
    /// </summary>
    public class MouseWheelZoomEventArgs : EventArgs
    {
        public int X { get; set; }
        public int Y { get; set; }
        public bool ZoomIn { get; set; }
    }

    /// <summary>
    /// Panel wyświetlający fraktal z obsługą interakcji.
    /// Nie skaluje istniejącego obrazu - każdy zoom wymaga nowego renderowania!
    /// </summary>
    public class MandelbrotPanel : Panel
    {
        private Bitmap _fractalImage;
        private bool _isSelecting;
        private Point _selectionStart;
        private Point _selectionEnd;

        public event EventHandler<ZoomEventArgs> ZoomRequested;
        public event EventHandler<MouseWheelZoomEventArgs> MouseWheelZoom;
        public event EventHandler RightClickBack;
        public event EventHandler<MouseEventArgs> MouseMoved;

        public MandelbrotPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            this.BackColor = Color.Black;
            this.Cursor = Cursors.Cross;
        }

        public bool HasImage => _fractalImage != null;

        /// <summary>
        /// Ustawia NOWY obraz fraktala (nie skaluje starego!).
        /// </summary>
        public void SetImage(Bitmap image)
        {
            var oldImage = _fractalImage;
            _fractalImage = image;
            oldImage?.Dispose();
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            if (_fractalImage != null)
            {
                // Rysowanie obrazu w pełnym rozmiarze panelu
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.DrawImage(_fractalImage, 0, 0, this.Width, this.Height);
            }
            else
            {
                // Placeholder
                g.Clear(Color.FromArgb(20, 20, 20));
                using (var font = new Font("Segoe UI", 16))
                using (var brush = new SolidBrush(Color.FromArgb(80, 80, 80)))
                {
                    string msg = "Kliknij 'RENDERUJ' aby wygenerować fraktal";
                    var size = g.MeasureString(msg, font);
                    g.DrawString(msg, font, brush,
                        (Width - size.Width) / 2, (Height - size.Height) / 2);
                }
            }

            // Rysowanie zaznaczenia
            if (_isSelecting)
            {
                DrawSelection(g);
            }
        }

        private void DrawSelection(Graphics g)
        {
            int x = Math.Min(_selectionStart.X, _selectionEnd.X);
            int y = Math.Min(_selectionStart.Y, _selectionEnd.Y);
            int w = Math.Abs(_selectionEnd.X - _selectionStart.X);
            int h = Math.Abs(_selectionEnd.Y - _selectionStart.Y);

            if (w < 2 || h < 2) return;

            // Przyciemnienie obszaru poza zaznaczeniem
            using (var dimBrush = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
            {
                // Górny pasek
                g.FillRectangle(dimBrush, 0, 0, Width, y);
                // Dolny pasek
                g.FillRectangle(dimBrush, 0, y + h, Width, Height - y - h);
                // Lewy pasek
                g.FillRectangle(dimBrush, 0, y, x, h);
                // Prawy pasek
                g.FillRectangle(dimBrush, x + w, y, Width - x - w, h);
            }

            // Ramka zaznaczenia
            using (var pen = new Pen(Color.FromArgb(255, 0, 180, 255), 2))
            {
                g.DrawRectangle(pen, x, y, w, h);
            }

            // Narożniki
            int cs = 10;
            using (var brush = new SolidBrush(Color.FromArgb(255, 0, 180, 255)))
            {
                g.FillRectangle(brush, x - cs/2, y - cs/2, cs, cs);
                g.FillRectangle(brush, x + w - cs/2, y - cs/2, cs, cs);
                g.FillRectangle(brush, x - cs/2, y + h - cs/2, cs, cs);
                g.FillRectangle(brush, x + w - cs/2, y + h - cs/2, cs, cs);
            }

            // Rozmiar zaznaczenia
            if (w > 80 && h > 50)
            {
                string info = $"{w} × {h} px";
                using (var font = new Font("Segoe UI", 10, FontStyle.Bold))
                {
                    var size = g.MeasureString(info, font);
                    float tx = x + (w - size.Width) / 2;
                    float ty = y + (h - size.Height) / 2;

                    using (var bgBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                    {
                        g.FillRectangle(bgBrush, tx - 5, ty - 2, size.Width + 10, size.Height + 4);
                    }
                    using (var textBrush = new SolidBrush(Color.White))
                    {
                        g.DrawString(info, font, textBrush, tx, ty);
                    }
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Left && _fractalImage != null)
            {
                _isSelecting = true;
                _selectionStart = e.Location;
                _selectionEnd = e.Location;
                this.Capture = true;
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
                _selectionEnd = new Point(
                    Math.Clamp(e.X, 0, Width),
                    Math.Clamp(e.Y, 0, Height));
                this.Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (_isSelecting && e.Button == MouseButtons.Left)
            {
                _isSelecting = false;
                this.Capture = false;

                int w = Math.Abs(_selectionEnd.X - _selectionStart.X);
                int h = Math.Abs(_selectionEnd.Y - _selectionStart.Y);

                if (w >= 10 && h >= 10)
                {
                    ZoomRequested?.Invoke(this, new ZoomEventArgs
                    {
                        StartX = _selectionStart.X,
                        StartY = _selectionStart.Y,
                        EndX = _selectionEnd.X,
                        EndY = _selectionEnd.Y
                    });
                }

                this.Invalidate();
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (_fractalImage != null)
            {
                MouseWheelZoom?.Invoke(this, new MouseWheelZoomEventArgs
                {
                    X = e.X,
                    Y = e.Y,
                    ZoomIn = e.Delta > 0
                });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fractalImage?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}