using System;
using System.Collections.Generic;

namespace Mandelbrot.Core
{
    /// <summary>
    /// Reprezentuje obszar widoku w przestrzeni liczb zespolonych.
    /// KLUCZOWA klasa - definiuje DOKŁADNIE jaki region przestrzeni zespolonej jest renderowany.
    /// Każda zmiana ViewPort = nowe renderowanie z nowymi współrzędnymi.
    /// </summary>
    public class ViewPort
    {
        /// <summary>
        /// Minimalna wartość części rzeczywistej (lewa granica)
        /// </summary>
        public double MinReal { get; set; }

        /// <summary>
        /// Maksymalna wartość części rzeczywistej (prawa granica)
        /// </summary>
        public double MaxReal { get; set; }

        /// <summary>
        /// Minimalna wartość części urojonej (dolna granica)
        /// </summary>
        public double MinImaginary { get; set; }

        /// <summary>
        /// Maksymalna wartość części urojonej (górna granica)
        /// </summary>
        public double MaxImaginary { get; set; }

        /// <summary>
        /// Domyślny widok - klasyczny obszar fraktala Mandelbrota
        /// </summary>
        public static ViewPort Default => new ViewPort
        {
            MinReal = -2.5,
            MaxReal = 1.0,
            MinImaginary = -1.5,
            MaxImaginary = 1.5
        };

        /// <summary>
        /// Szerokość obszaru w przestrzeni zespolonej
        /// </summary>
        public double Width => MaxReal - MinReal;

        /// <summary>
        /// Wysokość obszaru w przestrzeni zespolonej
        /// </summary>
        public double Height => MaxImaginary - MinImaginary;

        /// <summary>
        /// Środek obszaru - część rzeczywista
        /// </summary>
        public double CenterReal => (MinReal + MaxReal) / 2.0;

        /// <summary>
        /// Środek obszaru - część urojona
        /// </summary>
        public double CenterImaginary => (MinImaginary + MaxImaginary) / 2.0;

        /// <summary>
        /// Oblicza poziom zoomu względem domyślnego widoku
        /// </summary>
        public double CalculateZoomLevel()
        {
            return ViewPort.Default.Width / Width;
        }

        /// <summary>
        /// Sprawdza czy jesteśmy blisko limitu precyzji double.
        /// Double ma około 15-17 cyfr znaczących.
        /// Przy bardzo głębokim zoomie możemy tracić precyzję.
        /// </summary>
        public bool IsNearPrecisionLimit()
        {
            // Ostrzegamy gdy szerokość obszaru jest mniejsza niż 1e-13
            // (około 10^13 zoom)
            return Width < 1e-13;
        }

        /// <summary>
        /// Tworzy nowy ViewPort dla zaznaczonego prostokątnego obszaru.
        /// To jest KLUCZOWA metoda - definiuje nowe współrzędne do renderowania.
        /// </summary>
        /// <param name="x1">Początek zaznaczenia X (piksele)</param>
        /// <param name="y1">Początek zaznaczenia Y (piksele)</param>
        /// <param name="x2">Koniec zaznaczenia X (piksele)</param>
        /// <param name="y2">Koniec zaznaczenia Y (piksele)</param>
        /// <param name="imageWidth">Szerokość obrazu w pikselach</param>
        /// <param name="imageHeight">Wysokość obrazu w pikselach</param>
        /// <param name="maintainAspectRatio">Czy zachować proporcje</param>
        /// <returns>Nowy ViewPort z nowymi współrzędnymi</returns>
        public ViewPort Zoom(int x1, int y1, int x2, int y2, int imageWidth, int imageHeight, bool maintainAspectRatio = true)
        {
            // Normalizacja
            int left = Math.Min(x1, x2);
            int right = Math.Max(x1, x2);
            int top = Math.Min(y1, y2);
            int bottom = Math.Max(y1, y2);

            // Minimalne zaznaczenie
            if (right - left < 5 || bottom - top < 5)
            {
                return this;
            }

            // Przeliczanie pikseli na współrzędne zespolone
            double xScale = Width / imageWidth;
            double yScale = Height / imageHeight;

            double newMinReal = MinReal + left * xScale;
            double newMaxReal = MinReal + right * xScale;
            double newMaxImaginary = MaxImaginary - top * yScale;
            double newMinImaginary = MaxImaginary - bottom * yScale;

            // Zachowanie proporcji
            if (maintainAspectRatio)
            {
                double newWidth = newMaxReal - newMinReal;
                double newHeight = newMaxImaginary - newMinImaginary;
                double imageAspect = (double)imageWidth / imageHeight;
                double viewAspect = newWidth / newHeight;

                double centerReal = (newMinReal + newMaxReal) / 2.0;
                double centerImag = (newMinImaginary + newMaxImaginary) / 2.0;

                if (viewAspect > imageAspect)
                {
                    // Szersze niż obraz - dopasuj wysokość
                    newWidth = newHeight * imageAspect;
                }
                else
                {
                    // Wyższe niż obraz - dopasuj szerokość
                    newHeight = newWidth / imageAspect;
                }

                newMinReal = centerReal - newWidth / 2.0;
                newMaxReal = centerReal + newWidth / 2.0;
                newMinImaginary = centerImag - newHeight / 2.0;
                newMaxImaginary = centerImag + newHeight / 2.0;
            }

            return new ViewPort
            {
                MinReal = newMinReal,
                MaxReal = newMaxReal,
                MinImaginary = newMinImaginary,
                MaxImaginary = newMaxImaginary
            };
        }

        /// <summary>
        /// Zoom względem punktu (np. pozycji kursora) z określonym współczynnikiem.
        /// Używane do zoomu kółkiem myszy.
        /// </summary>
        /// <param name="centerX">Pozycja X w pikselach</param>
        /// <param name="centerY">Pozycja Y w pikselach</param>
        /// <param name="imageWidth">Szerokość obrazu</param>
        /// <param name="imageHeight">Wysokość obrazu</param>
        /// <param name="zoomFactor">Współczynnik zoomu (>1 = przybliżenie, <1 = oddalenie)</param>
        /// <returns>Nowy ViewPort</returns>
        public ViewPort ZoomAtPoint(int centerX, int centerY, int imageWidth, int imageHeight, double zoomFactor)
        {
            // Przeliczanie pozycji myszy na współrzędne zespolone
            double xScale = Width / imageWidth;
            double yScale = Height / imageHeight;

            double pointReal = MinReal + centerX * xScale;
            double pointImaginary = MaxImaginary - centerY * yScale;

            // Nowa szerokość i wysokość
            double newWidth = Width / zoomFactor;
            double newHeight = Height / zoomFactor;

            // Proporcje - punkt pod kursorem pozostaje w tym samym miejscu
            double leftRatio = (pointReal - MinReal) / Width;
            double topRatio = (MaxImaginary - pointImaginary) / Height;

            return new ViewPort
            {
                MinReal = pointReal - newWidth * leftRatio,
                MaxReal = pointReal + newWidth * (1 - leftRatio),
                MaxImaginary = pointImaginary + newHeight * topRatio,
                MinImaginary = pointImaginary - newHeight * (1 - topRatio)
            };
        }

        /// <summary>
        /// Przesuwa widok o określony wektor (w pikselach).
        /// </summary>
        public ViewPort Pan(int deltaX, int deltaY, int imageWidth, int imageHeight)
        {
            double xScale = Width / imageWidth;
            double yScale = Height / imageHeight;

            double deltaReal = -deltaX * xScale;
            double deltaImaginary = deltaY * yScale;

            return new ViewPort
            {
                MinReal = MinReal + deltaReal,
                MaxReal = MaxReal + deltaReal,
                MinImaginary = MinImaginary + deltaImaginary,
                MaxImaginary = MaxImaginary + deltaImaginary
            };
        }

        /// <summary>
        /// Tworzy kopię ViewPort.
        /// </summary>
        public ViewPort Clone()
        {
            return new ViewPort
            {
                MinReal = MinReal,
                MaxReal = MaxReal,
                MinImaginary = MinImaginary,
                MaxImaginary = MaxImaginary
            };
        }

        /// <summary>
        /// Tekstowa reprezentacja dla debugowania.
        /// </summary>
        public override string ToString()
        {
            return $"Re: [{MinReal:E4}, {MaxReal:E4}], Im: [{MinImaginary:E4}, {MaxImaginary:E4}], Zoom: {CalculateZoomLevel():E2}x";
        }
    }

    /// <summary>
    /// Historia widoków - pozwala na cofanie się do poprzednich poziomów zoomu.
    /// </summary>
    public class ViewPortHistory
    {
        private readonly Stack<ViewPort> _history;
        private readonly int _maxSize;

        public ViewPortHistory(int maxSize = 100)
        {
            _history = new Stack<ViewPort>();
            _maxSize = maxSize;
        }

        /// <summary>
        /// Czy można cofnąć się do poprzedniego widoku
        /// </summary>
        public bool CanGoBack => _history.Count > 0;

        /// <summary>
        /// Liczba zapisanych widoków
        /// </summary>
        public int Count => _history.Count;

        /// <summary>
        /// Zapisuje bieżący widok w historii.
        /// </summary>
        public void Push(ViewPort viewPort)
        {
            if (_history.Count >= _maxSize)
            {
                // Usuwanie najstarszych wpisów
                var temp = new Stack<ViewPort>();
                for (int i = 0; i < _maxSize - 1 && _history.Count > 0; i++)
                {
                    temp.Push(_history.Pop());
                }
                _history.Clear();
                while (temp.Count > 0)
                {
                    _history.Push(temp.Pop());
                }
            }
            _history.Push(viewPort.Clone());
        }

        /// <summary>
        /// Pobiera poprzedni widok.
        /// </summary>
        public ViewPort Pop()
        {
            return _history.Count > 0 ? _history.Pop() : ViewPort.Default;
        }

        /// <summary>
        /// Czyści historię.
        /// </summary>
        public void Clear()
        {
            _history.Clear();
        }
    }
}