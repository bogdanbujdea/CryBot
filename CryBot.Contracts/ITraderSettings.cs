namespace CryBot.Contracts
{
    public interface ITraderSettings
    {
        decimal HighStopLossPercentage { get; set; }
        
        decimal StopLoss { get; set; }
        
        decimal MinimumTakeProfit { get; set; }
        
        decimal BuyLowerPercentage { get; set; }
        
        decimal DefaultBudget { get; set; }
    }
}