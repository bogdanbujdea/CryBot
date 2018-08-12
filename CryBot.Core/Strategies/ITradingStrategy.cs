using CryBot.Core.Trader;
using CryBot.Core.Exchange.Models;

namespace CryBot.Core.Strategies
{
    public interface ITradingStrategy
    {
        string Name { get; }

        TraderSettings Settings { get; set; }

        TradeAction CalculateTradeAction(Ticker ticker, Trade trade);
    }
}