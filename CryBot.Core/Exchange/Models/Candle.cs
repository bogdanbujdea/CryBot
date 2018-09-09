using Bittrex.Net.Objects;

using CryBot.Core.Strategies;

using System;

namespace CryBot.Core.Exchange.Models
{
    public class Candle
    {
        public string Currency { get; set; }

        public DateTime Timestamp { get; set; }

        public decimal High { get; set; }

        public decimal Low { get; set; }

        public decimal Open { get; set; }

        public decimal Close { get; set; }

        public decimal Volume { get; set; }
        
        public TickInterval Interval { get; set; }
        
        public TradeAdvice EmaAdvice { get; set; }
    }
}