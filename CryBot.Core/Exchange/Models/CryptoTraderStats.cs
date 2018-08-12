namespace CryBot.Core.Exchange.Models
{
    public class CryptoTraderStats
    {
        public decimal InvestedBTC { get; set; }

        public decimal CurrentBTC { get; set; }

        public decimal Profit { get; set; }

        public decimal AvailableBTC { get; set; }

        public int Closed { get; set; }

        public int Opened { get; set; }
    }
}