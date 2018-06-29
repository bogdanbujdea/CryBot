namespace CryBot.Core.Models
{
    public class Trade
    {
        public bool IsActive { get; set; }

        public CryptoOrder BuyOrder { get; set; }

        public CryptoOrder SellOrder { get; set; }
        public decimal MaxPricePerUnit { get; set; }
    }
}