using System.Collections.Generic;

namespace CryBot.Core.Models
{
    public class Wallet
    {
        public CoinBalance BitcoinBalance { get; set; }

        public List<CoinBalance> Coins { get; set; }
    }
}
