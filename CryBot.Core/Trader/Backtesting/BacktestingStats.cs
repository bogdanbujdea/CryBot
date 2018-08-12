using CryBot.Core.Strategies;
using CryBot.Core.Exchange.Models;

namespace CryBot.Core.Trader.Backtesting
{
    public class BacktestingStats
    {
        public TraderSettings TraderSettings { get; set; }

        public ITradingStrategy TradingStrategy { get; set; }

        public CryptoTraderStats TraderStats { get; set; }

        public string Market { get; set; }
    }
}