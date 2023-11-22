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
    public partial class MainWindow : Window
    {
        private readonly BackgroundWorker _bgGenerateSignal, _bgResearch;
        private SignalGenerator _signalGenerator;
        private Dictionary<string, object> _params1, _params2;
        private Dictionary<string, int> _maxIndexes;

        public MainWindow()
        {
            InitializeComponent();

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
            // SetUpChart(ChartResearch, "Зависимость вероятности обнаружения сигнала от ОСШ", "Уровень шума, дБ", "Вероятность обнаружения");

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
                ["isNoise"] = CbIsNoise.IsChecked ?? false,
                ["SNR"] = NudSnr.Value ?? 5
            };

            // Получение битовой последовательности.
            var bitsSequence = new List<int>();
            TbBitsSequence.Text.Replace(" ", "").ToList().ForEach(b => bitsSequence.Add(b == '1' ? 1 : 0));
            _params1["bitsSequence"] = bitsSequence;

            ButtonGenerateSignal.IsEnabled = false;
            _bgGenerateSignal.RunWorkerAsync();
        }

        private void OnDoWorkBackgroundWorkerGenerateSignal(object sender, DoWorkEventArgs e)
        {
            try
            {
                _signalGenerator = new SignalGenerator(_params1);
                _signalGenerator.CalculatedIQComponents();
                // Наложение шума.
                if ((bool)_params1["isNoise"])
                    _signalGenerator.MakeNoise((double)_params1["SNR"]);
                _signalGenerator.CalculatedConvolution(out _maxIndexes);

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
            TbRestoredBitsSequence.Text = string.Join("", _signalGenerator.DecodeSignal());
            
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
                maxValues.Add(pair.Value[_maxIndexes[pair.Key]].Y);
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
            _params1["bitsSequence"] = bitsSequence;
            
            ProgressResearch.Value = 0;
            
            _bgResearch.RunWorkerAsync();
        }

        private void OnDoWorkBackgroundWorkerConductResearch(object sender, DoWorkEventArgs e)
        {
            try
            {
                
            }
            catch (Exception exception)
            {
                MessageBox.Show("Ошибка!", exception.Message);
            }
        }

        private void OnRunWorkerCompletedBackgroundWorkerConductResearch(object sender, RunWorkerCompletedEventArgs e) { }

        private void OnProgressChangedBackgroundWorkerConductResearch(object sender, ProgressChangedEventArgs e)
        {
            ChartIComponent.Visibility = Visibility.Collapsed;
            ChartQComponent.Visibility = Visibility.Collapsed;
            ChartComplexEnvelope.Visibility = Visibility.Collapsed;
            ChartConvolutions.Visibility = Visibility.Collapsed;
            ChartResearch.Visibility = Visibility.Visible;
            
            // Отрисовка графика ... . 
            ChartResearch.Plot.Clear();
            
            ButtonConductResearch.Visibility = Visibility.Visible;
            ProgressResearch.Visibility = Visibility.Collapsed;
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
            for (var i = 0; i < bits.Length; i++)
                TbBitsSequence.Text += bits[i];

            ButtonGenerateSignal.IsEnabled = true;
            OnGenerateSignal(null, null);
        }

        #endregion

        #region ################# ONCHECKED #################

        private void OnCheckedCheckBoxIsNoise(object sender, RoutedEventArgs e)
        {
            NudSnr.IsEnabled = CbIsNoise.IsChecked ?? false;
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