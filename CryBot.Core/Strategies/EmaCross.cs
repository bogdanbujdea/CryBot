using CryBot.Core.Exchange.Models;
using System.Linq;
using System.Collections.Generic;

namespace CryBot.Core.Strategies
{
    public class EmaCross : BaseStrategy
    {
        public override string Name => "EMA Cross";

        public EmaCross()
        {
            Candles = new List<Candle>();
        }

        public override int MinimumAmountOfCandles => 36;

        public override Period IdealPeriod => Period.Hour;

        public override List<TradeAdvice> Prepare(List<Candle> candles)
        {
            var result = new List<TradeAdvice>();

            List<decimal?> ema12 = candles.Ema(12);
            List<decimal?> ema26 = candles.Ema(26);

            for (int i = 0; i < candles.Count; i++)
            {
                if (i == 0)
                    result.Add(TradeAdvice.Hold);
                else if (ema12[i] < ema26[i] && ema12[i - 1] > ema26[i])
                    result.Add(TradeAdvice.Buy);
                else if (ema12[i] > ema26[i] && ema12[i - 1] < ema26[i])
                    result.Add(TradeAdvice.Sell);
                else
                    result.Add(TradeAdvice.Hold);
            }

            return result;
        }

        public override Candle GetSignalCandle(List<Candle> candles)
        {
            return candles.Last();
        }

        public override TradeAdvice Forecast(List<Candle> candles)
        {
            return Prepare(candles).LastOrDefault();
        }
    }
}
