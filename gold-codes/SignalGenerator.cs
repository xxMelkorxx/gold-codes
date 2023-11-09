using System;
using System.Linq;
using System.Collections.Generic;

namespace gold_codes;

public class SignalGenerator
{
    /// <summary>
    /// Битовая последовательность.
    /// </summary>
    private List<int> bitsSequence { get; }

    /// <summary>
    /// Длина битовой последовательности.
    /// </summary>
    public int Nb => bitsSequence.Count;

    /// <summary>
    /// Битрейт.
    /// </summary>
    public int BPS { get; }

    /// <summary>
    /// Частота дискретизации.
    /// </summary>
    public double fd { get; }

    /// <summary>
    /// Амплитуда несущего сигнала.
    /// </summary>
    public double a0 { get; }

    /// <summary>
    /// Частота несущего сигнала.
    /// </summary>
    public double f0 { get; }

    /// <summary>
    /// Начальная фаза несущего сигнала.
    /// </summary>
    public double phi0 { get; }

    /// <summary>
    /// Временной отрезок одного бита.
    /// </summary>
    public double tb => 1d / BPS;

    /// <summary>
    /// Шаг по времени.
    /// </summary>
    public double dt => 1d / fd;

    /// <summary>
    /// Число отсчётов в дискретном сигнала.
    /// </summary>
    private int countNumbers => (int)(Nb * tb / dt);

    /// <summary>
    /// Словарь кодов Голда.
    /// </summary>
    private Dictionary<string, int[]> _goldSequences;

    /// <summary>
    /// I-компонента сигнала.
    /// </summary>
    public List<PointD> IComponent;

    /// <summary>
    /// Q-компонента сигнала.
    /// </summary>
    public List<PointD> QComponent;

    public List<PointD> ComplexEnvelope;

    private const double pi2 = 2 * Math.PI;

    public SignalGenerator(IReadOnlyDictionary<string, object> @params)
    {
        BPS = (int)@params["bps"];
        fd = (double)@params["fd"];
        a0 = (double)@params["a0"];
        f0 = (double)@params["f0"];
        phi0 = (double)@params["phi0"];

        // Конвертация битовой последовательности в последовательность Голда.
        bitsSequence = new List<int>();
        ConvertToGoldSequence((List<int>)@params["bitsSequence"]);

        IComponent = new List<PointD>();
        QComponent = new List<PointD>();
        ComplexEnvelope = new List<PointD>();
    }

    /// <summary>
    /// Получение кодов Голда.
    /// </summary>
    /// <param name="m1"></param>
    /// <param name="m2"></param>
    /// <returns></returns>
    private static int[] GetGoldCode(int[] m1, int[] m2) => m1.Zip(m2, (i1, i2) => i1 ^ i2).ToArray();

    /// <summary>
    /// Получение сдвинутой последовательности.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="shift"></param>
    /// <returns></returns>
    private static int[] ShiftedArray(int[] input, int shift)
    {
        var shiftedArray = new int[input.Length];
        Array.Copy(input, input.Length - shift, shiftedArray, 0, shift);
        Array.Copy(input, 0, shiftedArray, shift, input.Length - shift);
        return shiftedArray;
    }

    /// <summary>
    /// Генерация m-последовательности.
    /// </summary>
    /// <param name="feedbackNumbers"></param>
    /// <returns></returns>
    private static int[] GenerateMSequence(int[] feedbackNumbers)
    {
        var n = feedbackNumbers.Length;

        var mSeqLength = (int)Math.Pow(2, n) - 1;
        var mSeq = new List<int>();
        var register = new int[n];
        register[1] = 1;

        for (var i = 0; i < mSeqLength; i++)
        {
            // Добавляем последнее значение в регистре в М последовательность.
            mSeq.Add(register[n - 1]);
            // Расчет обратной связи.
            var feedbackSum = feedbackNumbers.Zip(register, (x, h) => h * x).Sum() % 2;
            // Сдвиг в регистре.
            for (var j = n - 1; j >= 1; j--)
                register[j] = register[j - 1];
            register[0] = feedbackSum;
        }

        return mSeq.ToArray();
    }

    /// <summary>
    /// Конвертация последователности в последовательноть Голда.
    /// </summary>
    /// <param name="bits"></param>
    /// <returns></returns>
    public void ConvertToGoldSequence(List<int> bits)
    {
        var m1 = GenerateMSequence(new[] { 1, 0, 0, 0, 0, 1 });
        var m2 = GenerateMSequence(new[] { 1, 1, 0, 0, 1, 1 });
        _goldSequences = new Dictionary<string, int[]>
        {
            ["00"] = GetGoldCode(m1, ShiftedArray(m2, 0)),
            ["10"] = GetGoldCode(m1, ShiftedArray(m2, 10)),
            ["01"] = GetGoldCode(m1, ShiftedArray(m2, 20)),
            ["11"] = GetGoldCode(m1, ShiftedArray(m2, 30))
        };

        for (var i = 0; i < (bits.Count % 2 == 0 ? bits.Count : bits.Count - 1); i += 2)
            bitsSequence.AddRange(_goldSequences[$"{bits[i]}{bits[i + 1]}"]);
    }

    /// <summary>
    /// Вычисление I и Q компонент сигнала.
    /// </summary>
    public void CalculatedIQComponents()
    {
        var idx = 0;
        for (var i = 0; i < (Nb % 2 == 0 ? Nb : Nb - 1); i += 2)
        for (var n = 0; n < 2 * (tb / dt); n++)
        {
            var ti = idx * dt;
            var bi1 = bitsSequence[i] - 0.5;
            var bi2 = bitsSequence[i + 1] - 0.5;
            IComponent.Add(new PointD(ti, bi1));
            QComponent.Add(new PointD(ti, bi2));
            ComplexEnvelope.Add(new PointD(ti, bi1 * Math.Cos(pi2 * f0 * ti) - bi2 * Math.Sin(pi2 * f0 * ti)));
            idx++;
        }
    }

    /// <summary>
    /// Генерация случайного числа с нормальным распределением.
    /// </summary>
    /// <param name="min">Минимальное число (левая граница)</param>
    /// <param name="max">Максимальное число (правая граница)</param>
    /// <param name="n">Количество случайных чисел, которые необходимо суммировать для достижения нормального распределения</param>
    /// <returns>Случайное нормально распределённое число</returns>
    private static double GetNormalRandom(double min, double max, int n = 12)
    {
        var rnd = new Random(Guid.NewGuid().GetHashCode());
        var sum = 0d;
        for (var i = 0; i < n; i++)
            sum += rnd.NextDouble() * (max - min) + min;
        return sum / n;
    }

    /// <summary>
    /// Генерация отнормированного белого шума.
    /// </summary>
    /// <param name="countNumbers">Число отсчётов</param>
    /// <param name="energySignal">Энергия сигнала, на который накладывается шум</param>
    /// <param name="snrDb">Уровень шума в дБ</param>
    /// <returns></returns>
    private static IEnumerable<double> GenerateNoise(int countNumbers, double energySignal, double snrDb)
    {
        var noise = new List<double>();
        for (var i = 0; i < countNumbers; i++)
            noise.Add(GetNormalRandom(-1d, 1d));

        // Нормировка шума.
        var snr = Math.Pow(10, -snrDb / 10);
        var norm = Math.Sqrt(snr * energySignal / noise.Sum(y => y * y));

        return noise.Select(y => y * norm).ToList();
    }


    /// <summary>
    /// Наложить шум на сигнал.
    /// </summary>
    /// <param name="snrDb"></param>
    /// <returns></returns>
    public void MakeNoise(double snrDb)
    {
        // Наложение шума на исследуемый сигнал.
        ComplexEnvelope = GenerateNoise(ComplexEnvelope.Count, ComplexEnvelope.Sum(p => p.Y * p.Y), snrDb)
            .Zip(ComplexEnvelope, (n, p) => new PointD(p.X, p.Y + n))
            .ToList();
    }

    /// <summary>
    /// Генерация битовой последовательности.
    /// </summary>
    /// <param name="countBits">кол-во битов последовательности</param>
    /// <returns></returns>
    public static string GenerateBitsSequence(int countBits)
    {
        var rnd = new Random(Guid.NewGuid().GetHashCode());
        var bits = string.Empty;
        for (var i = 0; i < countBits; i++)
            bits += rnd.Next(0, 2);
        return bits;
    }
}