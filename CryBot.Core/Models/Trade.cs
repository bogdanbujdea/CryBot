using CryBot.Contracts;

namespace CryBot.Core.Models
{
    public class Trade: ITrade
    {
        public bool IsActive { get; set; }

        public ICryptoOrder BuyOrder { get; set; } = new CryptoOrder();
               
        public ICryptoOrder SellOrder { get; set; } = new CryptoOrder();

        public decimal MaxPricePerUnit { get; set; }
    }
}