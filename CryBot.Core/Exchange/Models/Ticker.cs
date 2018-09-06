using CryBot.Core.Strategies;

using System;

namespace CryBot.Core.Exchange.Models
{
    public class Ticker
    {
        public int Id { get; set; }

        public decimal Last { get; set; }

        public decimal Bid { get; set; }

        public decimal Ask { get; set; }
        
        public decimal BaseVolume { get; set; }
        
        public string Market { get; set; }

        public DateTime Timestamp { get; set; }

        public TradeAdvice LatestEmaAdvice { get; set; }

        public static Ticker FromPrice(decimal bid, decimal ask, decimal last, string market)
        {
            return new Ticker
            {
                Market = market,
                Ask = ask,
                Bid = bid,
                Last = last
            };
        }
    }
}