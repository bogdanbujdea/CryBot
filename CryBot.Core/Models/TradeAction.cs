namespace CryBot.Core.Models
{
    public class TradeAction
    {
        public TradeAdvice TradeAdvice { get; set; }

        public decimal OrderPricePerUnit { get; set; }

        public TradeReason Reason { get; set; }

        public static TradeAction Create(TradeAdvice tradeAdvice, TradeReason reason = TradeReason.None, decimal pricePerUnit = 0)
        {
            return new TradeAction { TradeAdvice = tradeAdvice, Reason = reason, OrderPricePerUnit = pricePerUnit };
        }
    }
}