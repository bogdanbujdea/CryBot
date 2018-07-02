namespace CryBot.Core.Models
{
    public class Trade
    {
        public bool IsActive { get; set; }

        public CryptoOrder BuyOrder { get; set; } = new CryptoOrder();

        public CryptoOrder SellOrder { get; set; } = new CryptoOrder();

        public decimal MaxPricePerUnit { get; set; }
    }
}