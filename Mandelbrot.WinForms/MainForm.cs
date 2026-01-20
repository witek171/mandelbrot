using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Mandelbrot.Core;
using Mandelbrot.Core.Calculators;

namespace Mandelbrot.WinForms
{
    public partial class MainForm : Form
    {
        private readonly CalculatorFactory _calculatorFactory;
        private readonly ColorPalette _colorPalette;
        private readonly ViewPortHistory _history;
        private ViewPort _currentViewPort;
        private IMandelbrotCalculator _currentCalculator;
        private bool _isRendering;

        // Kontrolki
        private MandelbrotPanel _mandelbrotPanel;
        private Panel _controlPanel;
        private NumericUpDown _iterationsNumeric;
        private ComboBox _calculatorComboBox;  // Nowe - wybór kalkulatora
        private ComboBox _paletteComboBox;
        private Button _renderButton;
        private Button _resetButton;
        private Button _backButton;
        private Label _timeLabel;
        private Label _calculatorInfoLabel;
        private Label _zoomLabel;
        private Label _coordinatesLabel;
        private Label _precisionWarningLabel;
        private ProgressBar _progressBar;
        private CheckBox _autoIterationsCheckBox;
        private NumericUpDown _threadsNumeric;
        private Label _threadInfoLabel;

        public MainForm()
        {
            _calculatorFactory = new CalculatorFactory();
            _colorPalette = new ColorPalette();
            _history = new ViewPortHistory();
            _currentViewPort = ViewPort.Default;
            _currentCalculator = _calculatorFactory.GetFastestCalculator();

            InitializeComponent();
            SetupUI();
        }

        private void SetupUI()
        {
            this.Text = "Fraktal Mandelbrota - GPU Accelerated";
            this.Size = new Size(1500, 950);
            this.MinimumSize = new Size(1100, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(25, 25, 28);

            // Panel kontrolny
            _controlPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 320,
                BackColor = Color.FromArgb(32, 32, 35),
                Padding = new Padding(15),
                AutoScroll = true
            };
            this.Controls.Add(_controlPanel);

            int y = 15;

            // Tytuł
            AddLabel("🌀 Fraktal Mandelbrota", 16, FontStyle.Bold, Color.White, ref y);
            AddLabel("GPU Accelerated Edition", 10, FontStyle.Italic, Color.Gray, ref y);
            y += 10;
            AddSeparator(ref y);

            // === SEKCJA: Silnik renderowania ===
            AddLabel("🚀 Silnik renderowania", 11, FontStyle.Bold, Color.FromArgb(100, 200, 255), ref y);
            y += 5;

            // ComboBox z kalkulatorami
            _calculatorComboBox = new ComboBox
            {
                Location = new Point(15, y),
                Size = new Size(285, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            // Dodanie dostępnych kalkulatorów
            foreach (var name in _calculatorFactory.AvailableCalculators)
            {
                _calculatorComboBox.Items.Add(name);
            }

            // Wybierz najszybszy jako domyślny
            var fastestName = _calculatorFactory.GetFastestCalculator().Name;
            _calculatorComboBox.SelectedItem = fastestName;
            _calculatorComboBox.SelectedIndexChanged += (s, e) =>
            {
                _currentCalculator = _calculatorFactory.GetCalculator(_calculatorComboBox.SelectedItem.ToString());
                UpdateCalculatorInfo();

                // --- NOWA LOGIKA BLOKOWANIA SUWAKA ---
                // Sprawdzamy czy nazwa zawiera "Parallel" (dostosuj do swoich nazw w menu)
                bool isParallel = _currentCalculator.Name.Contains("Parallel");

                _threadsNumeric.Enabled = isParallel;
                _threadInfoLabel.Visible = isParallel;

                // Wizualne wyszarzenie
                _threadsNumeric.BackColor = isParallel ? Color.FromArgb(45, 45, 48) : Color.FromArgb(30, 30, 30);
            };

            _controlPanel.Controls.Add(_calculatorComboBox);
            y += 35;

            // Info o kalkulatorze
            _calculatorInfoLabel = new Label
            {
                Text = GetCalculatorDescription(_currentCalculator.Name),
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(150, 150, 150),
                Location = new Point(15, y),
                Size = new Size(285, 35)
            };
            _controlPanel.Controls.Add(_calculatorInfoLabel);
            y += 40;

            // === NOWY KOD: Kontrola Wątków ===
            AddLabel("Liczba wątków CPU:", 9, FontStyle.Regular, Color.LightGray, ref y);

            int maxLogicalProcessors = Environment.ProcessorCount;

            _threadsNumeric = new NumericUpDown
            {
                Location = new Point(15, y),
                Size = new Size(140, 28),
                Minimum = 1,
                Maximum = 64, // Pozwalamy na więcej niż mamy (do testów!)
                Value = maxLogicalProcessors, // Domyślnie tyle ile ma Twój laptop
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };

            // Etykieta informacyjna obok
            _threadInfoLabel = new Label
            {
                Text = $"✅ Sprzętowo ({maxLogicalProcessors}/{maxLogicalProcessors})",
                Location = new Point(165, y + 3),
                AutoSize = true,
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.LightGreen
            };

            // Logika zmiany kolorów (Ostrzeganie o przeciążeniu)
            _threadsNumeric.ValueChanged += (s, e) =>
            {
                int selected = (int)_threadsNumeric.Value;
                if (selected <= maxLogicalProcessors)
                {
                    _threadInfoLabel.Text = $"✅ W ramach zasobów CPU ({selected}/{maxLogicalProcessors})";
                    _threadInfoLabel.ForeColor = Color.LightGreen;
                }
                else
                {
                    _threadInfoLabel.Text = "⚠ Przekracza liczbę procesorów logicznych";
                    _threadInfoLabel.ForeColor = Color.Orange;
                }
            };

            _controlPanel.Controls.Add(_threadsNumeric);
            _controlPanel.Controls.Add(_threadInfoLabel);
            y += 35;

            AddSeparator(ref y);

            // === SEKCJA: Parametry ===
            AddLabel("⚙️ Parametry", 11, FontStyle.Bold, Color.FromArgb(100, 200, 255), ref y);
            y += 5;

            AddLabel("Liczba iteracji:", 9, FontStyle.Regular, Color.LightGray, ref y);
            _iterationsNumeric = new NumericUpDown
            {
                Location = new Point(15, y),
                Size = new Size(285, 28),
                Minimum = 100,
                Maximum = 100000,
                Value = 1000,
                Increment = 100,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };
            _controlPanel.Controls.Add(_iterationsNumeric);
            y += 35;

            _autoIterationsCheckBox = new CheckBox
            {
                Text = "Auto-dostosuj do zoomu",
                Location = new Point(15, y),
                Size = new Size(285, 22),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.LightGray,
                Checked = true
            };
            _controlPanel.Controls.Add(_autoIterationsCheckBox);
            y += 30;

            // Paleta
            AddLabel("Paleta kolorów:", 9, FontStyle.Regular, Color.LightGray, ref y);
            _paletteComboBox = CreateComboBox(ref y, new[] {
                "🌈 Tęczowa", "🔥 Ogień", "🌊 Ocean", "⬛ Szarość",
                "⚡ Elektryczna", "🌅 Zachód słońca", "🌲 Las", "💡 Neon"
            });
            _paletteComboBox.SelectedIndex = 0;
            _paletteComboBox.SelectedIndexChanged += (s, e) =>
            {
                _colorPalette.CurrentPalette = (ColorPalette.PaletteType)_paletteComboBox.SelectedIndex;
            };
            y += 10;

            AddSeparator(ref y);

            // === SEKCJA: Przyciski ===
            AddLabel("🎮 Sterowanie", 11, FontStyle.Bold, Color.FromArgb(100, 200, 255), ref y);
            y += 5;

            _renderButton = CreateButton("▶️ RENDERUJ", Color.FromArgb(0, 120, 210), ref y, 50);
            _renderButton.Click += async (s, e) => await RenderFractalAsync();

            _backButton = CreateButton("⬅️ Cofnij zoom", Color.FromArgb(70, 70, 75), ref y, 38);
            _backButton.Click += async (s, e) => await GoBackAsync();
            _backButton.Enabled = false;

            _resetButton = CreateButton("🔄 Resetuj widok", Color.FromArgb(70, 70, 75), ref y, 38);
            _resetButton.Click += async (s, e) => await ResetViewAsync();

            y += 10;
            AddSeparator(ref y);

            // === SEKCJA: Statystyki ===
            AddLabel("📊 Statystyki", 11, FontStyle.Bold, Color.FromArgb(100, 200, 255), ref y);
            y += 5;

            AddLabel("Czas renderowania:", 9, FontStyle.Regular, Color.LightGray, ref y);
            _timeLabel = new Label
            {
                Text = "---",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 220, 150),
                Location = new Point(15, y),
                AutoSize = true
            };
            _controlPanel.Controls.Add(_timeLabel);
            y += 45;

            AddLabel("Poziom zoomu:", 9, FontStyle.Regular, Color.LightGray, ref y);
            _zoomLabel = new Label
            {
                Text = "1x",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 200, 80),
                Location = new Point(15, y),
                AutoSize = true
            };
            _controlPanel.Controls.Add(_zoomLabel);
            y += 40;

            _coordinatesLabel = new Label
            {
                Text = "📍 Najedź na fraktal",
                Font = new Font("Consolas", 8),
                ForeColor = Color.Gray,
                Location = new Point(15, y),
                Size = new Size(285, 35)
            };
            _controlPanel.Controls.Add(_coordinatesLabel);
            y += 40;

            _precisionWarningLabel = new Label
            {
                Text = "⚠️ Limit precyzji double!\nRozważ bibliotekę arbitrary precision.",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(255, 180, 50),
                Location = new Point(15, y),
                Size = new Size(285, 35),
                Visible = false
            };
            _controlPanel.Controls.Add(_precisionWarningLabel);
            y += 40;

            AddSeparator(ref y);

            // Instrukcje
            AddLabel("💡 Instrukcje", 11, FontStyle.Bold, Color.FromArgb(100, 200, 255), ref y);
            var instructionLabel = new Label
            {
                Text = "• Zaznacz obszar myszą → zoom\n" +
                       "• Scroll → szybki zoom\n" +
                       "• Prawy przycisk → cofnij\n" +
                       "• GPU = 50-150x szybciej!",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(130, 130, 130),
                Location = new Point(15, y),
                Size = new Size(285, 80)
            };
            _controlPanel.Controls.Add(instructionLabel);
            y += 85;

            _progressBar = new ProgressBar
            {
                Location = new Point(15, y),
                Size = new Size(285, 6),
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };
            _controlPanel.Controls.Add(_progressBar);

            // Panel fraktala
            _mandelbrotPanel = new MandelbrotPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black
            };
            _mandelbrotPanel.ZoomRequested += async (s, e) => await HandleZoomAsync(e);
            _mandelbrotPanel.MouseWheelZoom += async (s, e) => await HandleMouseWheelZoomAsync(e);
            _mandelbrotPanel.RightClickBack += async (s, e) => await GoBackAsync();
            _mandelbrotPanel.MouseMoved += UpdateCoordinates;
            this.Controls.Add(_mandelbrotPanel);

            _controlPanel.BringToFront();
        }

        private void UpdateCalculatorInfo()
        {
            _calculatorInfoLabel.Text = GetCalculatorDescription(_currentCalculator.Name);
        }

        private string GetCalculatorDescription(string name)
        {
            if (name.Contains("GPU"))
                return "🎮 Obliczenia na karcie graficznej\n   Najszybsza opcja!";
            if (name.Contains("SIMD"))
                return "⚡ 4 piksele na raz (AVX2)\n   Bardzo szybkie";
            if (name.Contains("Parallel"))
                return "🔄 Wielowątkowe CPU\n   Wykorzystuje wszystkie rdzenie";
            return "🔹 Podstawowe obliczenia\n   Najprostsze, ale najwolniejsze";
        }

        private async Task RenderFractalAsync()
        {
            if (_isRendering) return;
            _isRendering = true;
            SetUIState(false);
            _progressBar.Visible = true;

            try
            {
                int width = _mandelbrotPanel.Width;
                int height = _mandelbrotPanel.Height;

                int maxIterations = (int)_iterationsNumeric.Value;
                if (_autoIterationsCheckBox.Checked)
                {
                    double zoom = _currentViewPort.CalculateZoomLevel();
                    // BYŁO: int autoIter = (int)(300 + Math.Log10(zoom + 1) * 200);
                    // TERAZ: mniejszy mnożnik
                    int autoIter = (int)(200 + Math.Log10(zoom + 1) * 100);
                    maxIterations = Math.Max((int)_iterationsNumeric.Value, Math.Min(autoIter, 5000));
                }

                RenderResult result = await Task.Run(() =>
                    _currentCalculator.Render(width, height, _currentViewPort, maxIterations, _colorPalette));

                _mandelbrotPanel.SetImage(result.Bitmap);
                _timeLabel.Text = result.FormattedTime;
                _zoomLabel.Text = result.FormattedZoom;
                _precisionWarningLabel.Visible = _currentViewPort.IsNearPrecisionLimit();
                _backButton.Enabled = _history.CanGoBack;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _progressBar.Visible = false;
                SetUIState(true);
                _isRendering = false;
            }
        }

        private async Task HandleZoomAsync(ZoomEventArgs e)
        {
            _history.Push(_currentViewPort);
            _currentViewPort = _currentViewPort.Zoom(
                e.StartX, e.StartY, e.EndX, e.EndY,
                _mandelbrotPanel.Width, _mandelbrotPanel.Height, true);
            await RenderFractalAsync();
        }

        private async Task HandleMouseWheelZoomAsync(MouseWheelZoomEventArgs e)
        {
            _history.Push(_currentViewPort);
            double factor = e.ZoomIn ? 2.0 : 0.5;
            _currentViewPort = _currentViewPort.ZoomAtPoint(
                e.X, e.Y, _mandelbrotPanel.Width, _mandelbrotPanel.Height, factor);
            await RenderFractalAsync();
        }

        private async Task GoBackAsync()
        {
            if (!_history.CanGoBack) return;
            _currentViewPort = _history.Pop();
            await RenderFractalAsync();
        }

        private async Task ResetViewAsync()
        {
            _history.Clear();
            _currentViewPort = ViewPort.Default;
            await RenderFractalAsync();
        }

        private void UpdateCoordinates(object sender, MouseEventArgs e)
        {
            if (_mandelbrotPanel.Width == 0) return;
            double xScale = _currentViewPort.Width / _mandelbrotPanel.Width;
            double yScale = _currentViewPort.Height / _mandelbrotPanel.Height;
            double real = _currentViewPort.MinReal + e.X * xScale;
            double imag = _currentViewPort.MaxImaginary - e.Y * yScale;
            _coordinatesLabel.Text = $"📍 c = {real:G8}\n     + {imag:G8}i";
        }

        // Metody pomocnicze UI...
        private void AddLabel(string text, int size, FontStyle style, Color color, ref int y)
        {
            var label = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", size, style),
                ForeColor = color,
                Location = new Point(15, y),
                AutoSize = true
            };
            _controlPanel.Controls.Add(label);
            y += (int)(size * 2.2);
        }

        private void AddSeparator(ref int y)
        {
            var sep = new Panel
            {
                Location = new Point(15, y),
                Size = new Size(285, 1),
                BackColor = Color.FromArgb(55, 55, 58)
            };
            _controlPanel.Controls.Add(sep);
            y += 15;
        }

        private ComboBox CreateComboBox(ref int y, string[] items)
        {
            var combo = new ComboBox
            {
                Location = new Point(15, y),
                Size = new Size(285, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            combo.Items.AddRange(items);
            _controlPanel.Controls.Add(combo);
            y += 35;
            return combo;
        }

        private Button CreateButton(string text, Color color, ref int y, int height)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(15, y),
                Size = new Size(285, height),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            _controlPanel.Controls.Add(btn);
            y += height + 8;
            return btn;
        }

        private void SetUIState(bool enabled)
        {
            _renderButton.Enabled = enabled;
            _resetButton.Enabled = enabled;
            _backButton.Enabled = enabled && _history.CanGoBack;
            _iterationsNumeric.Enabled = enabled;
            _calculatorComboBox.Enabled = enabled;
            _paletteComboBox.Enabled = enabled;
            _mandelbrotPanel.Enabled = enabled;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // Pokaż info o systemie
            Console.WriteLine("\n=== INFORMACJE O SYSTEMIE ===");
            Console.WriteLine($"OS: {Environment.OSVersion}");
            Console.WriteLine($"CPU: {Environment.ProcessorCount} rdzeni");
            Console.WriteLine($".NET: {Environment.Version}");
            Console.WriteLine($"64-bit: {Environment.Is64BitProcess}");
            Console.WriteLine($"SIMD Vector<double> size: {System.Numerics.Vector<double>.Count}");

            // Test szybkości wszystkich kalkulatorów
            Console.WriteLine("\n=== BENCHMARK (100x100, 100 iter) ===");

            var testViewPort = ViewPort.Default;
            var testPalette = new ColorPalette();

            foreach (var name in _calculatorFactory.AvailableCalculators)
            {
                var calc = _calculatorFactory.GetCalculator(name);

                try
                {
                    // Rozgrzewka
                    calc.Render(100, 100, testViewPort, 100, testPalette)?.Bitmap?.Dispose();

                    // Pomiar
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    for (int i = 0; i < 3; i++)
                    {
                        calc.Render(100, 100, testViewPort, 100, testPalette)?.Bitmap?.Dispose();
                    }
                    sw.Stop();

                    Console.WriteLine($"  {name}: {sw.ElapsedMilliseconds / 3} ms");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  {name}: BŁĄD - {ex.Message}");
                }
            }

            Console.WriteLine("\n=================================\n");

            _ = RenderFractalAsync();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            _calculatorFactory.Dispose();
        }
    }
}