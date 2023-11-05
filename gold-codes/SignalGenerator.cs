using System;
using System.Linq;
using System.Collections.Generic;

namespace gold_codes;

public class SignalGenerator
{
    /// <summary>
    /// Битовая последовательность.
    /// </summary>
    private List<bool> bitsSequence { get; }

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
    /// I-компонента сигнала.
    /// </summary>
    public List<PointD> ISignal;
    
    /// <summary>
    /// Q-компонента сигнала.
    /// </summary>
    public List<PointD> QSignal;

    public SignalGenerator(IReadOnlyDictionary<string, object> @params)
    {
        bitsSequence = (List<bool>)@params["bitsSequence"];
        BPS = (int)@params["bps"];
        fd = (double)@params["fd"];
        a0 = (double)@params["a0"];
        f0 = (double)@params["f0"];
        phi0 = (double)@params["phi0"];
        
        ISignal = new List<PointD>();
        QSignal = new List<PointD>();
    }

    public void GenerateIQ()
    {
        
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
        // Наложение шума на искомый сигнал.
        // desiredSignal = desiredSignal.Zip(
        //         GenerateNoise(researchedSignal.Count, researchedSignal.Sum(p => p.Y * p.Y), snrDb),
        //         (p, n) => new PointD(p.X, p.Y + n))
        //     .ToList();

        // Наложение шума на исследуемый сигнал.
        // researchedSignal = researchedSignal.Zip(
        //         GenerateNoise(researchedSignal.Count, researchedSignal.Sum(p => p.Y * p.Y), snrDb),
        //         (p, n) => new PointD(p.X, p.Y + n))
        //     .ToList();
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
        for (var i = 8; i <= countBits - 8 - countBits % 8; i += 8)
            bits += Convert.ToString(rnd.Next(0, 255), 2).PadLeft(8, '0');
        bits += Convert.ToString(rnd.Next(0, (int)Math.Pow(2, 8 + countBits % 8) - 1), 2).PadLeft(8 + countBits % 8, '0');
        
        return bits;
    }
}