using CryBot.Core.Strategies;
using CryBot.Core.Exchange.Models;

namespace CryBot.Core.Trader
{
    public class Trade
    {
        public CryptoOrder BuyOrder { get; set; } = new CryptoOrder();

        public CryptoOrder SellOrder { get; set; } = new CryptoOrder();

        public decimal MaxPricePerUnit { get; set; }

        public string Market { get; set; }

        public decimal Profit { get; set; }

        public ITradingStrategy Strategy { get; set; }

        public Ticker CurrentTicker { get; set; } = new Ticker();

        public bool TriggeredBuy { get; set; }

        public TradeStatus Status { get; set; }

        public static Trade Empty { get; set; } = new Trade();
    }
}