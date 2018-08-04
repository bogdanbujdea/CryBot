namespace CryBot.Core.Models
{
    public class TradeAction
    {
        public TradeAdvice TradeAdvice { get; set; }

        public decimal OrderPricePerUnit { get; set; }

        public TradeReason Reason { get; set; }
    }

    public enum TradeReason
    {
        StopLoss,
        TakeProfit,
        BuyTrigger,
        ExpiredBuyOrder,
        FirstTrade
    }
}