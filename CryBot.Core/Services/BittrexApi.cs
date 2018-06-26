using Bittrex.Net;
using Bittrex.Net.Objects;

using CryBot.Core.Models;
using CryBot.Core.Utilities;

using CryptoExchange.Net.Authentication;

using System.Linq;

using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Core.Services
{
    public class BittrexApi : ICryptoApi
    {
        private BittrexClient _bittrexClient;

        public void Initialize(string apiKey, string apiSecret)
        {
            var bittrexClientOptions = new BittrexClientOptions
            {
                ApiCredentials = new ApiCredentials(apiKey, apiSecret)
            };

            _bittrexClient = new BittrexClient(bittrexClientOptions);
        }

        public async Task<Wallet> GetWalletAsync()
        {
            var wallet = new Wallet();
            var balances = await RetrieveBalances();
            var markets = await _bittrexClient.GetMarketSummariesAsync();
            wallet.Coins = balances;
            wallet.BitcoinBalance = wallet.Coins.FirstOrDefault(c => c.Currency == "BTC");
            foreach (var coinBalance in wallet.Coins)
            {
                var market = markets.Data.FirstOrDefault(m => m.MarketName == coinBalance.MarketName);
                if (market != null)
                {
                    coinBalance.PricePerUnit = market.Last.GetValueOrDefault();
                    coinBalance.Price = market.Last.GetValueOrDefault() * coinBalance.Balance;
                }

                if (wallet.BitcoinBalance != null) 
                    wallet.BitcoinBalance.Balance += coinBalance.Price;
            }

            return wallet;
        }

        private async Task<List<CoinBalance>> RetrieveBalances()
        {
            var balancesCallResult = await _bittrexClient.GetBalancesAsync();
            if (balancesCallResult != null && balancesCallResult.Success)
            {
                var coins = new List<CoinBalance>();
                foreach (var bittrexBalance in balancesCallResult.Data.Where(CoinIsValid))
                {
                    coins.Add(bittrexBalance.ConvertToCoinBalance());
                }
                return coins;
            }
            return new List<CoinBalance>();
        }

        private static bool CoinIsValid(BittrexBalance b)
        {
            return b.Currency != "USDT" && b.Balance > 0;
        }
    }
}
