namespace CryBot.Core.Models
{
    public interface ITradingStrategy
    {
        string Name { get; }

        TraderSettings Settings { get; set; }

        TradeAction CalculateTradeAction(Ticker ticker, Trade trade);
    }
}