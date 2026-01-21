using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Mandelbrot.Core.Calculators;
using Mandelbrot.Core.Caching;
using Mandelbrot.Core.History;
using Mandelbrot.Core.Pooling;
using Mandelbrot.Core.Rendering;
using Mandelbrot.WinForms.Controls;

namespace Mandelbrot.WinForms
{
    public partial class MainForm : Form
    {
        private readonly CalculatorFactory _calculatorFactory;
        private readonly IterationCache _iterationCache;
        private readonly BitmapPool _bitmapPool;
        private readonly Renderer _renderer;
        private readonly ColorPalette _colorPalette;
        private readonly ViewPortHistory _history;

        private ViewPort _currentViewPort;
        private IMandelbrotCalculator _currentCalculator;
        private bool _isRendering;

        // UI Controls
        private MandelbrotPanel _mandelbrotPanel;
        private Panel _controlPanel;
        private ComboBox _calculatorComboBox;
        private ComboBox _paletteComboBox;
        private NumericUpDown _iterationsNumeric;
        private NumericUpDown _threadsNumeric;
        private Label _threadsLabel;
        private Label _threadsWarningLabel;  // NOWY - ostrzeżenie
        private CheckBox _autoIterCheckBox;
        private Button _renderButton;
        private Button _resetButton;
        private Button _backButton;
        private Label _timeLabel;
        private Label _zoomLabel;
        private Label _coordsLabel;
        private Label _precisionLabel;
        private Label _cacheLabel;
        private ProgressBar _progressBar;

        private static readonly int MaxPhysicalCores = Environment.ProcessorCount;

        public MainForm()
        {
            _calculatorFactory = new CalculatorFactory();
            _iterationCache = new IterationCache(200);
            _bitmapPool = new BitmapPool(3);
            _renderer = new Renderer(_iterationCache, _bitmapPool);
            _colorPalette = new ColorPalette();
            _history = new ViewPortHistory();
            _currentViewPort = ViewPort.Default;

            InitializeComponent();
            SetupUI();
        }

        private void SetupUI()
        {
            Text = "Fraktal Mandelbrota";
            Size = new Size(1400, 900);
            MinimumSize = new Size(1000, 700);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(25, 25, 28);

            _controlPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 300,
                BackColor = Color.FromArgb(32, 32, 35),
                Padding = new Padding(15),
                AutoScroll = true
            };
            Controls.Add(_controlPanel);

            int y = 15;

            AddLabel("🌀 Mandelbrot", 14, FontStyle.Bold, Color.White, ref y);
            y += 5;
            AddSeparator(ref y);

            // ═══════════════════════════════════════════════════════════
            // KALKULATOR
            // ═══════════════════════════════════════════════════════════
            AddLabel("Silnik renderowania:", 9, FontStyle.Regular, Color.LightGray, ref y);
            _calculatorComboBox = CreateComboBox(ref y);
            foreach (var name in _calculatorFactory.AvailableCalculators)
                _calculatorComboBox.Items.Add(name);

            if (_calculatorComboBox.Items.Count > 0)
                _calculatorComboBox.SelectedIndex = 0;

            _calculatorComboBox.SelectedIndexChanged += OnCalculatorChanged;

            // ═══════════════════════════════════════════════════════════
            // WĄTKI (tylko dla Parallel)
            // ═══════════════════════════════════════════════════════════
            _threadsLabel = new Label
            {
                Text = $"Liczba wątków (max fizycznie: {MaxPhysicalCores}):",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.LightGray,
                Location = new Point(15, y),
                AutoSize = true,
                Visible = false
            };
            _controlPanel.Controls.Add(_threadsLabel);
            y += 22;

            _threadsNumeric = new NumericUpDown
            {
                Location = new Point(15, y),
                Size = new Size(265, 28),
                Minimum = 1,
                Maximum = MaxPhysicalCores * 4,  // Pozwól na więcej dla testów
                Value = MaxPhysicalCores,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                Visible = false
            };
            _threadsNumeric.ValueChanged += OnThreadCountChanged;
            _controlPanel.Controls.Add(_threadsNumeric);
            y += 32;

            // OSTRZEŻENIE O WĄTKACH
            _threadsWarningLabel = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8),
                Location = new Point(15, y),
                Size = new Size(265, 35),
                Visible = false
            };
            _controlPanel.Controls.Add(_threadsWarningLabel);
            y += 40;

            AddSeparator(ref y);

            // ═══════════════════════════════════════════════════════════
            // PALETA
            // ═══════════════════════════════════════════════════════════
            AddLabel("Paleta kolorów:", 9, FontStyle.Regular, Color.LightGray, ref y);
            _paletteComboBox = CreateComboBox(ref y);
            _paletteComboBox.Items.AddRange(new[] {
                "🌈 Tęczowa", "🔥 Ogień", "🌊 Ocean", "⬛ Szarość",
                "⚡ Elektryczna", "🌅 Zachód", "🌲 Las", "💡 Neon"
            });
            _paletteComboBox.SelectedIndex = 0;
            _paletteComboBox.SelectedIndexChanged += OnPaletteChanged;

            AddSeparator(ref y);

            // ═══════════════════════════════════════════════════════════
            // ITERACJE
            // ═══════════════════════════════════════════════════════════
            AddLabel("Iteracje:", 9, FontStyle.Regular, Color.LightGray, ref y);
            _iterationsNumeric = new NumericUpDown
            {
                Location = new Point(15, y),
                Size = new Size(265, 28),
                Minimum = 100,
                Maximum = 100000,
                Value = 1000,
                Increment = 100,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };
            _controlPanel.Controls.Add(_iterationsNumeric);
            y += 32;

            _autoIterCheckBox = new CheckBox
            {
                Text = "Auto-dostosuj do zoomu",
                Location = new Point(15, y),
                Size = new Size(265, 22),
                ForeColor = Color.LightGray,
                Checked = true
            };
            _controlPanel.Controls.Add(_autoIterCheckBox);
            y += 30;

            AddSeparator(ref y);

            // ═══════════════════════════════════════════════════════════
            // PRZYCISKI
            // ═══════════════════════════════════════════════════════════
            _renderButton = CreateButton("▶ RENDERUJ", Color.FromArgb(0, 120, 210), ref y);
            _renderButton.Click += async (s, e) => await RenderAsync();

            _backButton = CreateButton("⬅ Cofnij", Color.FromArgb(70, 70, 75), ref y);
            _backButton.Click += async (s, e) => await GoBackAsync();
            _backButton.Enabled = false;

            _resetButton = CreateButton("🔄 Reset", Color.FromArgb(70, 70, 75), ref y);
            _resetButton.Click += async (s, e) => await ResetAsync();

            y += 10;
            AddSeparator(ref y);

            // ═══════════════════════════════════════════════════════════
            // STATYSTYKI
            // ═══════════════════════════════════════════════════════════
            AddLabel("Czas:", 9, FontStyle.Regular, Color.LightGray, ref y);
            _timeLabel = new Label
            {
                Text = "---",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 220, 150),
                Location = new Point(15, y),
                AutoSize = true
            };
            _controlPanel.Controls.Add(_timeLabel);
            y += 40;

            AddLabel("Zoom:", 9, FontStyle.Regular, Color.LightGray, ref y);
            _zoomLabel = new Label
            {
                Text = "1×",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 200, 80),
                Location = new Point(15, y),
                AutoSize = true
            };
            _controlPanel.Controls.Add(_zoomLabel);
            y += 35;

            _precisionLabel = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Red,
                Location = new Point(15, y),
                Size = new Size(265, 20)
            };
            _controlPanel.Controls.Add(_precisionLabel);
            y += 25;

            _coordsLabel = new Label
            {
                Text = "📍 Najedź na fraktal",
                Font = new Font("Consolas", 8),
                ForeColor = Color.Gray,
                Location = new Point(15, y),
                Size = new Size(265, 30)
            };
            _controlPanel.Controls.Add(_coordsLabel);
            y += 35;

            _cacheLabel = new Label
            {
                Text = "Cache: 0",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.DimGray,
                Location = new Point(15, y),
                AutoSize = true
            };
            _controlPanel.Controls.Add(_cacheLabel);
            y += 25;

            _progressBar = new ProgressBar
            {
                Location = new Point(15, y),
                Size = new Size(265, 4),
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };
            _controlPanel.Controls.Add(_progressBar);

            // ═══════════════════════════════════════════════════════════
            // PANEL FRAKTALA
            // ═══════════════════════════════════════════════════════════
            _mandelbrotPanel = new MandelbrotPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black
            };
            _mandelbrotPanel.ZoomRequested += async (s, e) => await ZoomAsync(e);
            _mandelbrotPanel.MouseWheelZoom += async (s, e) => await WheelZoomAsync(e);
            _mandelbrotPanel.RightClickBack += async (s, e) => await GoBackAsync();
            _mandelbrotPanel.MouseMoved += UpdateCoords;
            Controls.Add(_mandelbrotPanel);

            _controlPanel.BringToFront();

            // Ustaw domyślny kalkulator
            if (_calculatorComboBox.Items.Count > 0)
            {
                string firstName = _calculatorComboBox.Items[0].ToString();
                _currentCalculator = _calculatorFactory.GetCalculator(firstName);
                UpdateThreadsVisibility();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // ZMIANA KALKULATORA
        // ═══════════════════════════════════════════════════════════════
        private void OnCalculatorChanged(object sender, EventArgs e)
        {
            if (_calculatorComboBox.SelectedItem == null) return;

            string name = _calculatorComboBox.SelectedItem.ToString();
            _currentCalculator = _calculatorFactory.GetCalculator(name);

            UpdateThreadsVisibility();
        }

        private void UpdateThreadsVisibility()
        {
            bool isParallel = _currentCalculator?.Name.Contains("Parallel") == true;
            _threadsLabel.Visible = isParallel;
            _threadsNumeric.Visible = isParallel;
            _threadsWarningLabel.Visible = isParallel;

            if (isParallel)
            {
                UpdateThreadWarning();
                ApplyThreadCount();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // ZMIANA LICZBY WĄTKÓW
        // ═══════════════════════════════════════════════════════════════
        private void OnThreadCountChanged(object sender, EventArgs e)
        {
            UpdateThreadWarning();
            ApplyThreadCount();
        }

        private void UpdateThreadWarning()
        {
            int selected = (int)_threadsNumeric.Value;

            if (selected > MaxPhysicalCores)
            {
                _threadsWarningLabel.Text = $"⚠️ Przekroczono liczbę rdzeni!\n" +
                                            $"Fizyczne: {MaxPhysicalCores}, wybrano: {selected}";
                _threadsWarningLabel.ForeColor = Color.Orange;
                _threadsNumeric.BackColor = Color.FromArgb(80, 60, 30);
            }
            else if (selected == MaxPhysicalCores)
            {
                _threadsWarningLabel.Text = $"✅ Optymalna liczba wątków ({selected})";
                _threadsWarningLabel.ForeColor = Color.LightGreen;
                _threadsNumeric.BackColor = Color.FromArgb(45, 45, 48);
            }
            else
            {
                _threadsWarningLabel.Text = $"ℹ️ Używasz {selected}/{MaxPhysicalCores} rdzeni";
                _threadsWarningLabel.ForeColor = Color.LightGray;
                _threadsNumeric.BackColor = Color.FromArgb(45, 45, 48);
            }
        }

        private void ApplyThreadCount()
        {
            if (_currentCalculator is CpuParallelCalculator parallel)
            {
                parallel.ThreadCount = (int)_threadsNumeric.Value;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // ZMIANA PALETY
        // ═══════════════════════════════════════════════════════════════
        private void OnPaletteChanged(object sender, EventArgs e)
        {
            _colorPalette.CurrentPalette = (ColorPalette.PaletteType)_paletteComboBox.SelectedIndex;

            var result = _renderer.RecolorWithPalette(_colorPalette);
            if (result != null)
            {
                _mandelbrotPanel.SetImage(result.Bitmap);
                _timeLabel.Text = result.FormattedTime + " ⚡";
                _timeLabel.ForeColor = Color.Cyan;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // RENDEROWANIE
        // ═══════════════════════════════════════════════════════════════
        private async Task RenderAsync()
        {
            if (_isRendering || _currentCalculator == null) return;
            _isRendering = true;

            _renderButton.Enabled = false;
            _calculatorComboBox.Enabled = false;
            _progressBar.Visible = true;

            try
            {
                int width = _mandelbrotPanel.Width;
                int height = _mandelbrotPanel.Height;
                int maxIter = CalculateIterations();

                var result = await Task.Run(() =>
                    _renderer.Render(_currentCalculator, width, height,
                        _currentViewPort, maxIter, _colorPalette));

                _mandelbrotPanel.SetImage(result.Bitmap);
                _timeLabel.Text = result.FormattedTime;
                _timeLabel.ForeColor = Color.FromArgb(50, 220, 150);

                UpdateLabels();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _progressBar.Visible = false;
                _renderButton.Enabled = true;
                _calculatorComboBox.Enabled = true;
                _isRendering = false;
            }
        }

        private int CalculateIterations()
        {
            int baseIter = (int)_iterationsNumeric.Value;

            if (_autoIterCheckBox.Checked)
            {
                double zoom = _currentViewPort.CalculateZoomLevel();
                int autoIter = (int)(200 + Math.Log10(zoom + 1) * 200);
                return Math.Max(baseIter, Math.Min(autoIter, 50000));
            }

            return baseIter;
        }

        // ═══════════════════════════════════════════════════════════════
        // ZOOM
        // ═══════════════════════════════════════════════════════════════
        private async Task ZoomAsync(ZoomEventArgs e)
        {
            _history.Push(_currentViewPort);
            _currentViewPort = _currentViewPort.Zoom(
                e.StartX, e.StartY, e.EndX, e.EndY,
                _mandelbrotPanel.Width, _mandelbrotPanel.Height, true);
            await RenderAsync();
        }

        private async Task WheelZoomAsync(MouseWheelZoomEventArgs e)
        {
            _history.Push(_currentViewPort);
            double factor = e.ZoomIn ? 2.0 : 0.5;
            _currentViewPort = _currentViewPort.ZoomAtPoint(
                e.X, e.Y, _mandelbrotPanel.Width, _mandelbrotPanel.Height, factor);
            await RenderAsync();
        }

        private async Task GoBackAsync()
        {
            if (!_history.CanGoBack) return;
            _currentViewPort = _history.Pop();
            await RenderAsync();
        }

        private async Task ResetAsync()
        {
            _history.Clear();
            _currentViewPort = ViewPort.Default;
            await RenderAsync();
        }

        // ═══════════════════════════════════════════════════════════════
        // AKTUALIZACJA UI
        // ═══════════════════════════════════════════════════════════════
        private void UpdateCoords(object sender, MouseEventArgs e)
        {
            if (_mandelbrotPanel.Width == 0) return;

            double xScale = _currentViewPort.Width / _mandelbrotPanel.Width;
            double yScale = _currentViewPort.Height / _mandelbrotPanel.Height;
            double real = _currentViewPort.MinReal + e.X * xScale;
            double imag = _currentViewPort.MaxImaginary - e.Y * yScale;

            _coordsLabel.Text = $"📍 {real:G8} + {imag:G8}i";
        }

        private void UpdateLabels()
        {
            double zoom = _currentViewPort.CalculateZoomLevel();
            _zoomLabel.Text = new RenderResult(null, TimeSpan.Zero, zoom).FormattedZoom;
            _backButton.Enabled = _history.CanGoBack;
            _cacheLabel.Text = $"Cache: {_iterationCache.Count} ({_iterationCache.CurrentMemoryMB}MB)";

            if (_currentCalculator is GpuCalculator gpuCalc)
            {
                if (!gpuCalc.UsesDouble)
                {
                    if (zoom > 900000)
                    {
                        _precisionLabel.Text = "⚠️ Limit precyzji GPU (32-bit float)";
                        _precisionLabel.ForeColor = Color.Red;
                    }
                    else
                    {
                        _precisionLabel.Text = "GPU (32-bit Float)";
                        _precisionLabel.ForeColor = Color.DimGray;
                    }
                }
                else
                {
                    if (zoom > 1e14)
                    {
                        _precisionLabel.Text = "⚠️ Limit precyzji Double";
                        _precisionLabel.ForeColor = Color.Yellow;
                    }
                    else
                    {
                        _precisionLabel.Text = "GPU (64-bit Double)";
                        _precisionLabel.ForeColor = Color.LightGreen;
                    }
                }
            }
            else
            {
                if (zoom > 1e14)
                {
                    _precisionLabel.Text = "⚠️ Limit precyzji CPU";
                    _precisionLabel.ForeColor = Color.Yellow;
                }
                else
                {
                    _precisionLabel.Text = "CPU (Double)";
                    _precisionLabel.ForeColor = Color.White;
                }
            }
        }
        #region UI Helpers

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
                Size = new Size(265, 1),
                BackColor = Color.FromArgb(55, 55, 58)
            };
            _controlPanel.Controls.Add(sep);
            y += 15;
        }

        private ComboBox CreateComboBox(ref int y)
        {
            var combo = new ComboBox
            {
                Location = new Point(15, y),
                Size = new Size(265, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _controlPanel.Controls.Add(combo);
            y += 32;
            return combo;
        }

        private Button CreateButton(string text, Color color, ref int y)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(15, y),
                Size = new Size(265, 38),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            _controlPanel.Controls.Add(btn);
            y += 45;
            return btn;
        }

        #endregion

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            await RenderAsync();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            _calculatorFactory.Dispose();
            _iterationCache.Dispose();
            _bitmapPool.Dispose();
            _renderer.Dispose();
        }
    }
}