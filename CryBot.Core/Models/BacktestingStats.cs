namespace CryBot.Core.Models
{
    public class BacktestingStats
    {
        public TraderSettings TraderSettings { get; set; }

        public ITradingStrategy TradingStrategy { get; set; }

        public CryptoTraderStats TraderStats { get; set; }

        public string Market { get; set; }
    }
}