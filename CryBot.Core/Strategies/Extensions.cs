using CryBot.Core.Exchange.Models;

using System;
using System.Linq;
using System.Collections.Generic;

namespace CryBot.Core.Strategies
{
    public static class Extensions
    {
        public static List<decimal?> Ema(this List<Candle> source, int period = 30, CandleVariable type = CandleVariable.Close)
        {
            double[] emaValues = new double[source.Count];
            double[] valuesToCheck;

            switch (type)
            {
                case CandleVariable.Open:
                    valuesToCheck = source.Select(x => Convert.ToDouble(x.Open)).ToArray();
                    break;
                case CandleVariable.Low:
                    valuesToCheck = source.Select(x => Convert.ToDouble(x.Low)).ToArray();
                    break;
                case CandleVariable.High:
                    valuesToCheck = source.Select(x => Convert.ToDouble(x.High)).ToArray();
                    break;
                default:
                    valuesToCheck = source.Select(x => Convert.ToDouble(x.Close)).ToArray();
                    break;
            }

            var ema = TicTacTec.TA.Library.Core.Ema(0, source.Count - 1, valuesToCheck, period, out var outBegIdx, out var outNbElement, emaValues);

            if (ema == TicTacTec.TA.Library.Core.RetCode.Success)
            {
                return FixIndicatorOrdering(emaValues.ToList(), outBegIdx, outNbElement);
            }

            throw new Exception("Could not calculate EMA!");
        }

        public static List<decimal?> Ema(this List<decimal> source, int period = 30)
        {
            double[] emaValues = new double[source.Count];
            List<double?> outValues = new List<double?>();

            var sourceFix = source.Select(Convert.ToDouble).ToArray();

            var sma = TicTacTec.TA.Library.Core.Ema(0, source.Count - 1, sourceFix, period, out var outBegIdx, out var outNbElement, emaValues);

            if (sma == TicTacTec.TA.Library.Core.RetCode.Success)
            {
                return FixIndicatorOrdering(emaValues.ToList(), outBegIdx, outNbElement);
            }

            throw new Exception("Could not calculate EMA!");
        }

        public static List<decimal?> Ema(this List<decimal?> source, int period = 30)
        {
            double[] emaValues = new double[source.Count];
            List<double?> outValues = new List<double?>();

            var sourceFix = source.Select(x => x.HasValue ? Convert.ToDouble(x) : 0).ToArray();

            var sma = TicTacTec.TA.Library.Core.Ema(0, source.Count - 1, sourceFix, period, out var outBegIdx, out var outNbElement, emaValues);

            if (sma == TicTacTec.TA.Library.Core.RetCode.Success)
            {
                return FixIndicatorOrdering(emaValues.ToList(), outBegIdx, outNbElement);
            }

            throw new Exception("Could not calculate EMA!");
        }

        private static List<decimal?> FixIndicatorOrdering(List<double> items, int outBegIdx, int outNbElement)
        {
            var outValues = new List<decimal?>();
            var validItems = items.Take(outNbElement);

            for (int i = 0; i < outBegIdx; i++)
                outValues.Add(null);

            foreach (var value in validItems)
                outValues.Add((decimal?)value);

            return outValues;
        }
    }
}
