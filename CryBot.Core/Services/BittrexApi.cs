using Bittrex.Net;
using Bittrex.Net.Objects;
using Bittrex.Net.Interfaces;

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
        private IBittrexClient _bittrexClient;

        public BittrexApi(IBittrexClient bittrexClient)
        {
            _bittrexClient = bittrexClient;
        }

        public void Initialize(string apiKey, string apiSecret)
        {
            _bittrexClient = new BittrexClient(new BittrexClientOptions
            {
                ApiCredentials = new ApiCredentials(apiKey, apiSecret)
            });
        }

        public async Task<CryptoResponse<Wallet>> GetWalletAsync()
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
                    coinBalance.Price = market.Last.GetValueOrDefault() * coinBalance.Quantity;
                }

                if (wallet.BitcoinBalance != null) 
                    wallet.BitcoinBalance.Quantity += coinBalance.Price;
            }

            return new CryptoResponse<Wallet>(wallet);
        }

        public async Task<CryptoResponse<List<CryptoOrder>>> GetOpenOrdersAsync()
        {
            var ordersCallResult = await _bittrexClient.GetOpenOrdersAsync();
            if (ordersCallResult.Success == false)
            {
                return new CryptoResponse<List<CryptoOrder>>(ordersCallResult.Error.Message);
            }
            var orders = ordersCallResult.Data.Select(s => s.ToCryptoOrder()).ToList();
            return new CryptoResponse<List<CryptoOrder>>(orders);
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
