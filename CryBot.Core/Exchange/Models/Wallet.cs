using System.Collections.Generic;

namespace CryBot.Core.Exchange.Models
{
    public class Wallet
    {
        public CoinBalance BitcoinBalance { get; set; }

        public List<CoinBalance> Coins { get; set; }
    }
}
