using System;
using System.Drawing;

namespace Mandelbrot.Core
{
    /// <summary>
    /// Klasa odpowiedzialna za generowanie kolorów dla wizualizacji fraktala.
    /// Obsługuje płynne kolorowanie (smooth coloring) dla lepszej jakości obrazu.
    /// </summary>
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

        private PaletteType _currentPalette = PaletteType.Rainbow;
        private Color[] _cachedPalette;
        private int _cachedSize;
        private const int PALETTE_SIZE = 2048; // Większa paleta = płynniejsze przejścia

        public PaletteType CurrentPalette
        {
            get => _currentPalette;
            set
            {
                _currentPalette = value;
                _cachedPalette = null; // Invalidacja cache
            }
        }

        /// <summary>
        /// Zwraca kolor dla gładkiej wartości iteracji.
        /// Używa interpolacji między kolorami dla płynnych przejść.
        /// </summary>
        /// <param name="smoothValue">Gładka wartość iteracji (może być ułamkowa)</param>
        /// <param name="maxIterations">Maksymalna liczba iteracji</param>
        /// <returns>Interpolowany kolor</returns>
        public Color GetSmoothColor(double smoothValue, int maxIterations)
        {
            // Punkt wewnątrz zbioru - czarny
            if (smoothValue >= maxIterations)
            {
                return Color.Black;
            }

            // Generowanie/pobieranie palety
            if (_cachedPalette == null || _cachedSize != PALETTE_SIZE)
            {
                _cachedPalette = GeneratePalette(PALETTE_SIZE);
                _cachedSize = PALETTE_SIZE;
            }

            // Mapowanie smooth value na indeks w palecie z interpolacją
            double colorIndex = smoothValue * 3.0; // Mnożnik dla większej zmienności kolorów
            colorIndex = colorIndex % PALETTE_SIZE;

            int index1 = (int)colorIndex % PALETTE_SIZE;
            int index2 = (index1 + 1) % PALETTE_SIZE;
            double fraction = colorIndex - Math.Floor(colorIndex);

            // Interpolacja liniowa między dwoma kolorami
            Color c1 = _cachedPalette[index1];
            Color c2 = _cachedPalette[index2];

            int r = (int)(c1.R + (c2.R - c1.R) * fraction);
            int g = (int)(c1.G + (c2.G - c1.G) * fraction);
            int b = (int)(c1.B + (c2.B - c1.B) * fraction);

            return Color.FromArgb(255,
                Math.Clamp(r, 0, 255),
                Math.Clamp(g, 0, 255),
                Math.Clamp(b, 0, 255));
        }

        /// <summary>
        /// Generuje pełną paletę kolorów.
        /// </summary>
        public Color[] GeneratePalette(int size)
        {
            return _currentPalette switch
            {
                PaletteType.Rainbow => GenerateRainbowPalette(size),
                PaletteType.Fire => GenerateFirePalette(size),
                PaletteType.Ocean => GenerateOceanPalette(size),
                PaletteType.Grayscale => GenerateGrayscalePalette(size),
                PaletteType.Electric => GenerateElectricPalette(size),
                PaletteType.Sunset => GenerateSunsetPalette(size),
                PaletteType.Forest => GenerateForestPalette(size),
                PaletteType.Neon => GenerateNeonPalette(size),
                _ => GenerateRainbowPalette(size)
            };
        }

        /// <summary>
        /// Paleta tęczowa - pełne spektrum HSV
        /// </summary>
        private Color[] GenerateRainbowPalette(int size)
        {
            Color[] palette = new Color[size];
            for (int i = 0; i < size; i++)
            {
                double hue = (double)i / size * 360.0;
                palette[i] = HsvToRgb(hue, 1.0, 1.0);
            }
            return palette;
        }

        /// <summary>
        /// Paleta ognia - od ciemnego czerwonego przez pomarańczowy do białego
        /// </summary>
        private Color[] GenerateFirePalette(int size)
        {
            Color[] palette = new Color[size];
            for (int i = 0; i < size; i++)
            {
                double t = (double)i / size;

                // Płynne przejście: czarny -> czerwony -> pomarańczowy -> żółty -> biały
                double r, g, b;
                if (t < 0.33)
                {
                    double lt = t / 0.33;
                    r = lt;
                    g = 0;
                    b = 0;
                }
                else if (t < 0.66)
                {
                    double lt = (t - 0.33) / 0.33;
                    r = 1.0;
                    g = lt;
                    b = 0;
                }
                else
                {
                    double lt = (t - 0.66) / 0.34;
                    r = 1.0;
                    g = 1.0;
                    b = lt;
                }

                palette[i] = Color.FromArgb(255,
                    (int)(r * 255),
                    (int)(g * 255),
                    (int)(b * 255));
            }
            return palette;
        }

        /// <summary>
        /// Paleta oceaniczna - odcienie błękitu i turkusu
        /// </summary>
        private Color[] GenerateOceanPalette(int size)
        {
            Color[] palette = new Color[size];
            for (int i = 0; i < size; i++)
            {
                double t = (double)i / size;

                // Sinusoidalne modulacje dla organicznego wyglądu
                double r = 0.1 + 0.3 * Math.Sin(t * Math.PI * 2);
                double g = 0.3 + 0.4 * Math.Sin(t * Math.PI * 2 + 1);
                double b = 0.5 + 0.5 * Math.Sin(t * Math.PI * 2 + 2);

                palette[i] = Color.FromArgb(255,
                    (int)(Math.Max(0, r) * 255),
                    (int)(Math.Max(0, g) * 255),
                    (int)(Math.Max(0, b) * 255));
            }
            return palette;
        }

        /// <summary>
        /// Paleta szarości
        /// </summary>
        private Color[] GenerateGrayscalePalette(int size)
        {
            Color[] palette = new Color[size];
            for (int i = 0; i < size; i++)
            {
                int gray = (int)((double)i / size * 255);
                palette[i] = Color.FromArgb(255, gray, gray, gray);
            }
            return palette;
        }

        /// <summary>
        /// Paleta elektryczna - neonowe kolory
        /// </summary>
        private Color[] GenerateElectricPalette(int size)
        {
            Color[] palette = new Color[size];
            for (int i = 0; i < size; i++)
            {
                double t = (double)i / size * Math.PI * 4;

                int r = (int)((Math.Sin(t) * 0.5 + 0.5) * 255);
                int g = (int)((Math.Sin(t + 2.094) * 0.5 + 0.5) * 255);
                int b = (int)((Math.Sin(t + 4.188) * 0.5 + 0.5) * 255);

                palette[i] = Color.FromArgb(255, r, g, b);
            }
            return palette;
        }

        /// <summary>
        /// Paleta zachodu słońca - od fioletu przez róż do pomarańczy
        /// </summary>
        private Color[] GenerateSunsetPalette(int size)
        {
            Color[] palette = new Color[size];

            // Kolory klucze zachodu słońca
            Color[] keyColors = {
                Color.FromArgb(255, 25, 25, 112),    // Midnight blue
                Color.FromArgb(255, 138, 43, 226),   // Blue violet
                Color.FromArgb(255, 255, 20, 147),   // Deep pink
                Color.FromArgb(255, 255, 99, 71),    // Tomato
                Color.FromArgb(255, 255, 165, 0),    // Orange
                Color.FromArgb(255, 255, 215, 0),    // Gold
                Color.FromArgb(255, 25, 25, 112)     // Back to start
            };

            for (int i = 0; i < size; i++)
            {
                double t = (double)i / size * (keyColors.Length - 1);
                int index = (int)t;
                double fraction = t - index;

                Color c1 = keyColors[index];
                Color c2 = keyColors[Math.Min(index + 1, keyColors.Length - 1)];

                palette[i] = Color.FromArgb(255,
                    (int)(c1.R + (c2.R - c1.R) * fraction),
                    (int)(c1.G + (c2.G - c1.G) * fraction),
                    (int)(c1.B + (c2.B - c1.B) * fraction));
            }
            return palette;
        }

        /// <summary>
        /// Paleta leśna - odcienie zieleni i brązu
        /// </summary>
        private Color[] GenerateForestPalette(int size)
        {
            Color[] palette = new Color[size];
            for (int i = 0; i < size; i++)
            {
                double t = (double)i / size;

                double r = 0.2 + 0.3 * Math.Sin(t * Math.PI * 3);
                double g = 0.4 + 0.4 * Math.Sin(t * Math.PI * 2 + 0.5);
                double b = 0.1 + 0.2 * Math.Sin(t * Math.PI * 4);

                palette[i] = Color.FromArgb(255,
                    (int)(Math.Clamp(r, 0, 1) * 255),
                    (int)(Math.Clamp(g, 0, 1) * 255),
                    (int)(Math.Clamp(b, 0, 1) * 255));
            }
            return palette;
        }

        /// <summary>
        /// Paleta neonowa - jaskrawe kontrastowe kolory
        /// </summary>
        private Color[] GenerateNeonPalette(int size)
        {
            Color[] palette = new Color[size];

            Color[] neonColors = {
                Color.FromArgb(255, 255, 0, 255),   // Magenta
                Color.FromArgb(255, 0, 255, 255),   // Cyan
                Color.FromArgb(255, 0, 255, 0),     // Green
                Color.FromArgb(255, 255, 255, 0),   // Yellow
                Color.FromArgb(255, 255, 0, 128),   // Hot pink
                Color.FromArgb(255, 0, 128, 255),   // Azure
                Color.FromArgb(255, 255, 0, 255)    // Back to start
            };

            for (int i = 0; i < size; i++)
            {
                double t = (double)i / size * (neonColors.Length - 1);
                int index = (int)t;
                double fraction = t - index;

                Color c1 = neonColors[index];
                Color c2 = neonColors[Math.Min(index + 1, neonColors.Length - 1)];

                palette[i] = Color.FromArgb(255,
                    (int)(c1.R + (c2.R - c1.R) * fraction),
                    (int)(c1.G + (c2.G - c1.G) * fraction),
                    (int)(c1.B + (c2.B - c1.B) * fraction));
            }
            return palette;
        }

        /// <summary>
        /// Konwersja HSV do RGB
        /// </summary>
        private Color HsvToRgb(double hue, double saturation, double value)
        {
            int hi = (int)(hue / 60.0) % 6;
            double f = hue / 60.0 - Math.Floor(hue / 60.0);

            double v = value * 255;
            int vInt = (int)v;
            int p = (int)(v * (1 - saturation));
            int q = (int)(v * (1 - f * saturation));
            int t = (int)(v * (1 - (1 - f) * saturation));

            return hi switch
            {
                0 => Color.FromArgb(255, vInt, t, p),
                1 => Color.FromArgb(255, q, vInt, p),
                2 => Color.FromArgb(255, p, vInt, t),
                3 => Color.FromArgb(255, p, q, vInt),
                4 => Color.FromArgb(255, t, p, vInt),
                _ => Color.FromArgb(255, vInt, p, q)
            };
        }
    }
}