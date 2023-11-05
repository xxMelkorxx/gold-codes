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