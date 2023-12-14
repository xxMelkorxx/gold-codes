using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ScottPlot;
using ScottPlot.Control;

namespace gold_codes
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly BackgroundWorker _bgGenerateSignal, _bgResearch;
        private SignalGenerator _signalGenerator;
        private Dictionary<string, object> _params1, _params2;
        private Dictionary<string, int[]> _goldCodes;
        private List<PointD> _berOnSnr;
        private bool _isNoise;

        public MainWindow()
        {
            InitializeComponent();

            _goldCodes = SignalGenerator.GetGoldCodes();

            _bgGenerateSignal = (BackgroundWorker)FindResource("BackgroundWorkerGenerateSignal");
            _bgResearch = (BackgroundWorker)FindResource("BackgroundWorkerConductResearch");
        }

        private void OnLoadedMainWindow(object sender, RoutedEventArgs e)
        {
            // Настройка графиков.
            SetUpChart(ChartIComponent, "I-компонента сигнала", "Время, с", "Амплитуда");
            SetUpChart(ChartQComponent, "Q-компонента сигнала", "Время, с", "Амплитуда");
            SetUpChart(ChartComplexEnvelope, "Комплексная огибающая", "Время, с", "Амплитуда");
            SetUpChart(ChartConvolutions, "Свёртки согласованнных фильтров", "Время, с", "Амплитуда");
            SetUpChart(ChartResearch, "Зависимость частоты возникновения ошибки от ОСШ", "Уровень шума, дБ", "Частота возникновения ошибки");

            OnClickButtonGenerateBitsSequence(null, null);
            OnGenerateSignal(null, null);
        }

        #region ################# GENERATE SIGNALS #################

        private void OnGenerateSignal(object sender, EventArgs e)
        {
            if (_bgGenerateSignal.IsBusy)
                return;

            _params1 = new Dictionary<string, object>
            {
                ["bps"] = NudBps.Value ?? 10,
                ["a0"] = NudA0.Value ?? 1,
                ["f0"] = NudF0.Value ?? 1000,
                ["phi0"] = NudPhi0.Value ?? 0,
                ["fd"] = NudFd.Value ?? 1,
                ["SNR"] = NudSnr.Value ?? 5
            };

            // Получение битовой последовательности.
            var bitsSequence = new List<int>();
            TbBitsSequence.Text.Replace(" ", "").ToList().ForEach(b => bitsSequence.Add(b == '1' ? 1 : 0));
            _params1["bitsSequence"] = bitsSequence;

            TbDecodeBitsSequence.Clear();
            ButtonGenerateSignal.IsEnabled = false;

            _bgGenerateSignal.RunWorkerAsync();
        }

        private void OnDoWorkBackgroundWorkerGenerateSignal(object sender, DoWorkEventArgs e)
        {
            try
            {
                _signalGenerator = new SignalGenerator(_params1);
                _signalGenerator.ConvertToGoldSequence(_goldCodes);
                _signalGenerator.CalculatedIQComponents();
                if (_isNoise) // Наложение шума.
                    _signalGenerator.MakeNoise((double)_params1["SNR"]);
                _signalGenerator.CalculatedConvolution(_goldCodes);
            }
            catch (Exception exception)
            {
                MessageBox.Show("Ошибка!", exception.Message);
            }
        }

        private void OnRunWorkerCompletedBackgroundWorkerGenerateSignal(object sender, RunWorkerCompletedEventArgs e)
        {
            ButtonGenerateSignal.IsEnabled = true;

            // Получение декодированной последовательности.
            TbDecodeBitsSequence.Text = string.Join("", _signalGenerator.DecodeSignal(out _));

            ChartIComponent.Visibility = Visibility.Visible;
            ChartQComponent.Visibility = Visibility.Visible;
            ChartComplexEnvelope.Visibility = Visibility.Visible;
            ChartConvolutions.Visibility = Visibility.Visible;
            ChartResearch.Visibility = Visibility.Collapsed;

            // Очистка графиков.
            ChartIComponent.Plot.Clear();
            ChartQComponent.Plot.Clear();
            ChartComplexEnvelope.Plot.Clear();
            ChartConvolutions.Plot.Clear();

            // График I-компоненты.
            ChartIComponent.Plot.AddSignalXY(
                xs: _signalGenerator.IComponent.Select(p => p.X).ToArray(),
                ys: _signalGenerator.IComponent.Select(p => p.Y).ToArray(),
                color: Color.Blue,
                label: "I-компонента"
            );
            ChartIComponent.Plot.Legend();
            ChartIComponent.Plot.SetAxisLimits(xMin: 0, xMax: _signalGenerator.IComponent.Max(p => p.X), yMin: -1, yMax: 1);
            ChartIComponent.Refresh();

            // График Q-компоненты.
            ChartQComponent.Plot.AddSignalXY(
                xs: _signalGenerator.QComponent.Select(p => p.X).ToArray(),
                ys: _signalGenerator.QComponent.Select(p => p.Y).ToArray(),
                color: Color.Red,
                label: "Q-компонента"
            );
            ChartQComponent.Plot.Legend();
            ChartQComponent.Plot.SetAxisLimits(xMin: 0, xMax: _signalGenerator.QComponent.Max(p => p.X), yMin: -1, yMax: 1);
            ChartQComponent.Refresh();

            // График комплексной огибающей.
            var yMax = 1.5 * _signalGenerator.ComplexEnvelope.Max(p => p.Y);
            ChartComplexEnvelope.Plot.AddSignalXY(
                xs: _signalGenerator.ComplexEnvelope.Select(p => p.X).ToArray(),
                ys: _signalGenerator.ComplexEnvelope.Select(p => p.Y).ToArray(),
                color: Color.Green,
                label: "Комплексная огибающая"
            );
            ChartComplexEnvelope.Plot.Legend();
            ChartComplexEnvelope.Plot.SetAxisLimits(xMin: 0, xMax: _signalGenerator.ComplexEnvelope.Max(p => p.X), yMin: -yMax, yMax: yMax);
            ChartComplexEnvelope.Refresh();

            // Графики свёрток согласованных фильтров.
            var maxValues = new List<double>();
            foreach (var pair in _signalGenerator.Convolutions)
            {
                ChartConvolutions.Plot.AddSignalXY(
                    pair.Value.Select(p => p.X).ToArray(),
                    pair.Value.Select(p => p.Y).ToArray(),
                    label: pair.Key
                );
                maxValues.Add(pair.Value.Max(p => p.Y));
            }

            yMax = maxValues.Max() * 1.5;
            ChartConvolutions.Plot.SetAxisLimits(yMin: -yMax, yMax: yMax);
            ChartConvolutions.Plot.Legend();
            ChartConvolutions.Refresh();
        }

        #endregion

        #region ################# CONDUCT RESEARCH #################

        private void OnClickButtonConductResearch(object sender, RoutedEventArgs e)
        {
            ButtonConductResearch.Visibility = Visibility.Collapsed;
            ProgressResearch.Visibility = Visibility.Visible;

            _berOnSnr = new List<PointD>();
            _params2 = new Dictionary<string, object>
            {
                ["bps"] = NudBps.Value ?? 10,
                ["a0"] = NudA0.Value ?? 1,
                ["f0"] = NudF0.Value ?? 1000,
                ["phi0"] = NudPhi0.Value ?? 0,
                ["fd"] = NudFd.Value ?? 1,
                ["meanOrder"] = NudMeanOrder.Value ?? 50,
                ["snrFrom"] = NudSnrFrom.Value ?? -20,
                ["snrTo"] = NudSnrTo.Value ?? 10,
                ["snrStep"] = NudSnrStep.Value ?? 0.5,
            };

            // Получение битовой последовательности.
            var bitsSequence = new List<int>();
            TbBitsSequence.Text.Replace(" ", "").ToList().ForEach(b => bitsSequence.Add(b == '1' ? 1 : 0));
            _params2["bitsSequence"] = bitsSequence;

            ProgressResearch.Value = 0;
            ProgressResearch.Maximum = (int)_params2["meanOrder"] * (((int)_params2["snrTo"] - (int)_params2["snrFrom"]) / (double)_params2["snrStep"] + 1);

            _bgResearch.RunWorkerAsync();
        }

        private void OnDoWorkBackgroundWorkerConductResearch(object sender, DoWorkEventArgs e)
        {
            try
            {
                var meanOrder = (int)_params2["meanOrder"];
                var snrFrom = (int)_params2["snrFrom"];
                var snrTo = (int)_params2["snrTo"];
                var snrStep = (double)_params2["snrStep"];
                var index = 0;

                Parallel.For(0, (int)((snrTo - snrFrom) / snrStep + 2), n =>
                {
                    var snr = snrFrom + n * snrStep;
                    var bers = 0d;
                    Parallel.For(0, meanOrder, i =>
                    {
                        var sg = new SignalGenerator(_params1);
                        sg.ConvertToGoldSequence(_goldCodes);
                        sg.CalculatedIQComponents();
                        sg.MakeNoise(snr);
                        sg.CalculatedConvolution(_goldCodes);
                        _ = sg.DecodeSignal(out var ber);
                        bers += ber;

                        // Обновление ProgressBar.
                        _bgResearch.ReportProgress(++index);
                    });
                    _berOnSnr.Add(new PointD(snr, bers / meanOrder));
                });
                _berOnSnr = _berOnSnr.OrderBy(p => p.X).ToList();
            }
            catch (Exception exception)
            {
                MessageBox.Show("Ошибка!", exception.Message);
            }
        }

        private void OnRunWorkerCompletedBackgroundWorkerConductResearch(object sender, RunWorkerCompletedEventArgs e)
        {
            ChartIComponent.Visibility = Visibility.Collapsed;
            ChartQComponent.Visibility = Visibility.Collapsed;
            ChartComplexEnvelope.Visibility = Visibility.Collapsed;
            ChartConvolutions.Visibility = Visibility.Collapsed;
            ChartResearch.Visibility = Visibility.Visible;

            // Отрисовка графика зависимости BER от SNR. 
            ChartResearch.Plot.Clear();
            ChartResearch.Plot.AddSignalXY(
                _berOnSnr.Select(p => p.X).ToArray(),
                _berOnSnr.Select(p => p.Y).ToArray(),
                Color.Blue,
                "BER(SNR)"
            );
            ChartResearch.Plot.Legend();
            ChartResearch.Plot.SetAxisLimits(xMin: (int)_params2["snrFrom"], xMax: (int)_params2["snrTo"], yMin: 0, yMax: 0.5);
            ChartResearch.Refresh();

            ButtonConductResearch.Visibility = Visibility.Visible;
            ProgressResearch.Visibility = Visibility.Collapsed;
        }

        private void OnProgressChangedBackgroundWorkerConductResearch(object sender, ProgressChangedEventArgs e)
        {
            ProgressResearch.Value = e.ProgressPercentage;
        }

        #endregion

        #region ################# GENERATE BIT SEQUENCE #################

        private void OnClickButtonAddZero(object sender, RoutedEventArgs e)
        {
            TbBitsSequence.Text += '0';
            ButtonGenerateSignal.IsEnabled = true;
        }

        private void OnClickButtonAddOne(object sender, RoutedEventArgs e)
        {
            TbBitsSequence.Text += '1';
            ButtonGenerateSignal.IsEnabled = true;
        }

        private void OnClickButtonClearBits(object sender, RoutedEventArgs e)
        {
            TbBitsSequence.Clear();
            ButtonGenerateSignal.IsEnabled = false;
        }

        private void OnClickButtonGenerateBitsSequence(object sender, RoutedEventArgs e)
        {
            var length = NudNb.Value ?? 16;
            var bits = SignalGenerator.GenerateBitsSequence(length);

            TbBitsSequence.Clear();
            TbBitsSequence.Text = bits;

            ButtonGenerateSignal.IsEnabled = true;
            OnGenerateSignal(null, null);
        }

        #endregion

        #region ################# ONCHECKED #################

        private void OnCheckedCheckBoxIsNoise(object sender, RoutedEventArgs e)
        {
            NudSnr.IsEnabled = CbIsNoise.IsChecked ?? false;
            _isNoise = CbIsNoise.IsChecked ?? false;
            OnGenerateSignal(null, null);
        }

        #endregion

        private static void SetUpChart(IPlotControl chart, string title, string labelX, string labelY)
        {
            chart.Plot.Title(title);
            chart.Plot.XLabel(labelX);
            chart.Plot.YLabel(labelY);
            chart.Plot.XAxis.MajorGrid(enable: true, color: Color.FromArgb(50, Color.Black));
            chart.Plot.YAxis.MajorGrid(enable: true, color: Color.FromArgb(50, Color.Black));
            chart.Plot.XAxis.MinorGrid(enable: true, color: Color.FromArgb(30, Color.Black), lineStyle: LineStyle.Dot);
            chart.Plot.YAxis.MinorGrid(enable: true, color: Color.FromArgb(30, Color.Black), lineStyle: LineStyle.Dot);
            chart.Plot.Margins(x: 0.0, y: 0.8);
            chart.Plot.SetAxisLimits(xMin: 0);
            chart.Configuration.Quality = QualityMode.High;
            chart.Configuration.DpiStretch = false;
            chart.Refresh();
        }
    }
}