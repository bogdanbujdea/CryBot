namespace CryBot.Core.Models
{
    public class CoinBalance
    {
        public decimal Quantity { get; set; }

        public decimal PricePerUnit { get; set; }

        public decimal Price { get; set; }

        public string Market { get; set; }

        public decimal Available { get; set; }
    }
}
