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
        private Dictionary<string, object> _params1;
        private int _maxIndex;

        public MainWindow()
        {
            InitializeComponent();

            _bgGenerateSignal = (BackgroundWorker)FindResource("BackgroundWorkerGenerateSignal");
            _bgResearch = (BackgroundWorker)FindResource("BackgroundWorkerConductResearch");
        }

        private void OnLoadedMainWindow(object sender, RoutedEventArgs e)
        {
            // Настройка графиков.
            SetUpChart(ChartISignal, "I-компонента сигнала", "Время, с", "Амплитуда");
            SetUpChart(ChartQSignal, "Q-компонента сигнала", "Время, с", "Амплитуда");
            // SetUpChart(ChartResearchedSignal, "Исследуемый сигнал", "Время, с", "Амплитуда");
            // SetUpChart(ChartCrossCorrelation, "Взаимная корреляция сигналов", "Время, с", "Амплитуда");
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
                // ["startBit"] = NudStartBit.Value ?? 100,
                // ["countBits"] = NudCountBits.Value ?? 200,
                // ["modulationType"] = _modulationType,
                ["isNoise"] = CbIsNoise.IsChecked ?? false,
                ["SNR"] = NudSnr.Value ?? 5
            };

            // Получение битовой последовательности.
            var bitsSequence = new List<bool>();
            TbBitsSequence.Text.Replace(" ", "").ToList().ForEach(b => bitsSequence.Add(b == '1'));
            _params1["bitsSequence"] = bitsSequence;

            ButtonGenerateSignal.IsEnabled = false;
            _bgGenerateSignal.RunWorkerAsync();
        }

        private void OnDoWorkBackgroundWorkerGenerateSignal(object sender, DoWorkEventArgs e)
        {
            try
            {
                
            }
            catch (Exception exception)
            {
                MessageBox.Show("Ошибка!", exception.Message);
            }
        }

        private void OnRunWorkerCompletedBackgroundWorkerGenerateSignal(object sender, RunWorkerCompletedEventArgs e) { ButtonGenerateSignal.IsEnabled = true; }

        #endregion

        #region ################# CONDUCT RESEARCH #################

        private void OnClickButtonConductResearch(object sender, RoutedEventArgs e) { }

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

        private void OnProgressChangedBackgroundWorkerConductResearch(object sender, ProgressChangedEventArgs e) { }

        #endregion

        #region ################# GENERATE BIT SEQUENCE #################

        private void OnClickButtonAddZero(object sender, RoutedEventArgs e)
        {
            TbBitsSequence.Text += TbBitsSequence.Text.Length % 5 == 0 ? " " : "";
            TbBitsSequence.Text += '0';
            ButtonGenerateSignal.IsEnabled = true;
        }

        private void OnClickButtonAddOne(object sender, RoutedEventArgs e)
        {
            TbBitsSequence.Text += TbBitsSequence.Text.Length % 5 == 0 ? " " : "";
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
            {
                TbBitsSequence.Text += i % 4 == 0 ? " " : "";
                TbBitsSequence.Text += bits[i];
            }

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