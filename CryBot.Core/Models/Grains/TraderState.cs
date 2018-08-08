using System.Collections.Generic;

namespace CryBot.Core.Models.Grains
{
    public class TraderState
    {
        public List<Trade> Trades { get; set; }
        
        public string Market { get; set; }
        
        public Ticker CurrentTicker { get; set; }
        
        public TraderSettings Settings { get; set; }
    }
}