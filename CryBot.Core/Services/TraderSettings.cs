namespace CryBot.Core.Services
{
    public class TraderSettings
    {
        public decimal HighStopLossPercentage { get; set; }

        public decimal HighStopLoss { get; set; }

        public decimal StopLoss { get; set; }
        public decimal MinimumTakeProfit { get; set; }
    }
}