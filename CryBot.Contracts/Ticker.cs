namespace CryBot.Contracts
{
    public class Ticker
    {
        public decimal Last { get; set; }

        public decimal Bid { get; set; }

        public decimal Ask { get; set; }
        
        public decimal BaseVolume { get; set; }
        
        public string Market { get; set; }
    }
}