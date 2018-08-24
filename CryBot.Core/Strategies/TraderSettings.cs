using System;

namespace CryBot.Core.Strategies
{
    public class TraderSettings
    {
        public decimal HighStopLossPercentage { get; set; }

        public decimal StopLoss { get; set; }

        public decimal BuyTrigger { get; set; }
        
        public decimal MinimumTakeProfit { get; set; }
        
        public decimal BuyLowerPercentage { get; set; }
        
        public decimal TradingBudget { get; set; }

        public TimeSpan ExpirationTime { get; set; }

        public static TraderSettings Default { get; } = new TraderSettings
        {
            //{BLP: 0| MTP: 0| HSL: -5| SL: -4| BT: -2| ET: 1.00:00:00}
            BuyLowerPercentage = 0,
            TradingBudget = 0.0012M,
            MinimumTakeProfit = 0M,
            HighStopLossPercentage = -5M,
            StopLoss = -4,
            BuyTrigger = -2M,
            ExpirationTime = TimeSpan.FromHours(1)
        };


        public override string ToString()
        {
            return $"BLP: {BuyLowerPercentage}| MTP: {MinimumTakeProfit}| HSL: {HighStopLossPercentage}| SL: {StopLoss}| BT: {BuyTrigger}| ET: {ExpirationTime.ToString()}";
        }
    }
}