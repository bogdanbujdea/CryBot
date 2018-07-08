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
    }
}