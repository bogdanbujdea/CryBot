namespace CryBot.Contracts
{
    public class Trade
    {
        public bool IsActive { get; set; }

        public CryptoOrder BuyOrder { get; set; } = new CryptoOrder();

        public CryptoOrder SellOrder { get; set; } = new CryptoOrder();

        public decimal MaxPricePerUnit { get; set; }

        public string Market { get; set; }

        public decimal Profit { get; set; }

        public ITradingStrategy Strategy { get; set; }

        public decimal CurrentValue => BuyOrder.Quantity * CurrentTicker.Bid * Consts.BittrexCommission;
        
        public Ticker CurrentTicker { get; set; }
        public bool TriggeredBuy { get; set; }

        public TradeAction CalculateAction(Ticker ticker)
        {
            return Strategy.CalculateTradeAction(ticker);
        }
    }
}