using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gold_codes
{
    public static class GoldCodes
    {
        public static int[] GenerateMSequence(int[] feedbackNumbers)
        {
            List<int> MSeq = new();
            int N = feedbackNumbers.Length;
            int[] register = new int[N];
            register[1] = 1;
            int MSeqLength = (int)Math.Pow(2, N) - 1;

            for (int i = 0; i < MSeqLength; i++)
            {
                double feedBackSum = 0; //Переменная для хранения значения обратной связи
                //Добавляем последнее значение в регистре в М последовательность
                MSeq.Add(register[N - 1]);
                //Расчет обратной связи
                for (int j = N - 1; j >= 0; j--)
                {
                    feedBackSum += register[j] * feedbackNumbers[j];
                }
                feedBackSum %= 2;
                //Сдвиг в регистре
                for (int j = N - 1; j >= 1; j--)
                {
                    register[j] = register[j - 1];
                }
                //Первое значение в регистре теперь значение обратной связи
                register[0] = (int)feedBackSum;
            }
            return MSeq.ToArray();
        }
        public static int[] ShiftedArray(int[] input, int shift)
        {
            int[] shiftedArray = new int[input.Length]; // создаем новый массив для хранения сдвинутых элементов

            // Копируем элементы из исходного массива в сдвинутый массив
            Array.Copy(input, input.Length - shift, shiftedArray, 0, shift);
            Array.Copy(input, 0, shiftedArray, shift, input.Length - shift);
            return shiftedArray;
        }
        public static int[] GetGoldCode(int[] M1, int[] M2)
        {
            int N = M1.Length;
            int[] result = new int[N];
            for (int i = 0; i < N; i++)
            {
                result[i] = M1[i] ^ M2[i];
            }
            return result;
        }
        public static int[] ConvertToGoldSequence(int[] bits, Dictionary<string, int[]> goldCodes)
        {
            List<int> result = new();
            for (int i = 0; i < bits.Length; i += 2)
            {
                result.AddRange(goldCodes[bits[i].ToString() + bits[i + 1].ToString()]);
            }
            return result.ToArray();
        }
    }
}
