using CryBot.Contracts;

using System.Collections.Generic;

namespace CryBot.Core.Models
{
    public class TraderState
    {
        public List<ITrade> Trades { get; set; }

        public string Market { get; set; }
        
        public ITicker CurrentTicker { get; set; }
        
        public ITraderSettings Settings { get; set; }
    }
}