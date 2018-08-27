using CryBot.Core.Strategies;

namespace CryBot.Core.Trader.Backtesting
{
    public class BackTestResult
    {
        public TraderSettings Settings { get; set; }

        public Budget Budget { get; set; }
    }
}