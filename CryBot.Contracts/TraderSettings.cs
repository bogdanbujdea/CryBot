namespace CryBot.Contracts
{
    public class TraderSettings
    {
        public decimal HighStopLossPercentage { get; set; }

        public decimal StopLoss { get; set; }

        public decimal BuyTrigger { get; set; }
        
        public decimal MinimumTakeProfit { get; set; }
        
        public decimal BuyLowerPercentage { get; set; }
        
        public decimal DefaultBudget { get; set; }
        
        public static TraderSettings Default { get; } = new TraderSettings
        {
            BuyLowerPercentage = -2,
            DefaultBudget = 0.0012M,
            MinimumTakeProfit = 0M,
            HighStopLossPercentage = -1M,
            StopLoss = -4,
            BuyTrigger = -2M,
        };

        public override string ToString()
        {
            return $"BLP: {BuyLowerPercentage}| MTP: {MinimumTakeProfit}| HSL: {HighStopLossPercentage}| SL: {StopLoss}| BT: {BuyTrigger}";
        }
    }
}