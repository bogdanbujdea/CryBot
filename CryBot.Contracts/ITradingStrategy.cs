namespace CryBot.Contracts
{
    public interface ITradingStrategy
    {
        string Name { get; }

        TraderSettings Settings { get; set; }

        Trade CurrentTrade { get; set; }

        TradeAction CalculateTradeAction(Ticker ticker);
    }
}