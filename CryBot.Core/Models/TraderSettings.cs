namespace CryBot.Core.Models
{
    public class TraderSettings
    {
        public decimal HighStopLossPercentage { get; set; }

        public decimal StopLoss { get; set; }

        public decimal BuyTrigger { get; set; }
        
        public decimal MinimumTakeProfit { get; set; }
        
        public decimal BuyLowerPercentage { get; set; }
        
        public decimal TradingBudget { get; set; }
        
        public static TraderSettings Default { get; } = new TraderSettings
        {
            BuyLowerPercentage = -5,
            TradingBudget = 0.0012M,
            MinimumTakeProfit = 0M,
            HighStopLossPercentage = -0.5M,
            StopLoss = -2,
            BuyTrigger = -1M,
        };

        public override string ToString()
        {
            return $"BLP: {BuyLowerPercentage}| MTP: {MinimumTakeProfit}| HSL: {HighStopLossPercentage}| SL: {StopLoss}| BT: {BuyTrigger}";
        }
    }
}