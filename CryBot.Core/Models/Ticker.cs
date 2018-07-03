using CryBot.Contracts;

namespace CryBot.Core.Models
{
    public class Ticker: ITicker
    {
        public decimal Last { get; set; }

        public decimal Bid { get; set; }

        public decimal Ask { get; set; }
        
        public decimal BaseVolume { get; set; }
        
        public string Market { get; set; }
    }
}