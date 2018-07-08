using Bittrex.Net;
using Bittrex.Net.Objects;
using Bittrex.Net.Interfaces;

using CryBot.Contracts;

using CryBot.Core.Models;
using CryBot.Core.Utilities;

using CryptoExchange.Net.Authentication;

using System;

using System.Linq;

using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Core.Services
{
    public class BittrexApi : ICryptoApi
    {
        private IBittrexClient _bittrexClient;
        private BittrexSocketClient _bittrexSocketClient;

        public BittrexApi(IBittrexClient bittrexClient)
        {
            _bittrexClient = bittrexClient;
        }

        public event EventHandler<List<Ticker>> MarketsUpdated;
        public event EventHandler<CryptoOrder> OrderUpdated;

        public void Initialize(string apiKey, string apiSecret)
        {
            var apiCredentials = new ApiCredentials(apiKey, apiSecret);
            _bittrexClient = new BittrexClient(new BittrexClientOptions
            {
                ApiCredentials = apiCredentials
            });
            _bittrexSocketClient = new BittrexSocketClient(new BittrexSocketClientOptions
            {
                ApiCredentials = apiCredentials
            });
            _bittrexSocketClient.SubscribeToMarketSummariesUpdate(OnMarketsUpdate);
            _bittrexSocketClient.SubscribeToOrderUpdates(OnOrderUpdate);
        }

        public async Task<CryptoResponse<Wallet>> GetWalletAsync()
        {
            var wallet = new Wallet();
            var balances = await RetrieveBalances();
            var markets = await _bittrexClient.GetMarketSummariesAsync();
            wallet.Coins = balances;
            wallet.BitcoinBalance = wallet.Coins.FirstOrDefault(c => c.Market.ToCurrency() == "BTC");
            wallet.Coins.Remove(wallet.BitcoinBalance);
            foreach (var coinBalance in wallet.Coins)
            {
                var market = markets.Data.FirstOrDefault(m => m.MarketName == coinBalance.Market);
                if (market != null)
                {
                    coinBalance.PricePerUnit = market.Last.GetValueOrDefault().RoundSatoshi();
                    coinBalance.Price = market.Last.GetValueOrDefault().RoundSatoshi() * coinBalance.Quantity.RoundSatoshi();
                }
            }
            wallet.BitcoinBalance.Quantity = (wallet.BitcoinBalance.Available + wallet.Coins.Where(c => c.Market.ToCurrency() != "BTC").Sum(c => c.Price)).RoundSatoshi();

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

        public async Task<CryptoResponse<List<CryptoOrder>>> GetCompletedOrdersAsync()
        {
            var orderHistoryResponse = await _bittrexClient.GetOrderHistoryAsync();
            if (orderHistoryResponse.Success)
            {
                var cryptoOrders = orderHistoryResponse.Data
                    .Select(o => o.ToCryptoOrder()).ToList();
                return new CryptoResponse<List<CryptoOrder>>(cryptoOrders);
            }
            return new CryptoResponse<List<CryptoOrder>>(orderHistoryResponse.Error.Message);
        }

        public virtual async Task<CryptoResponse<CryptoOrder>> BuyCoinAsync(CryptoOrder cryptoOrder)
        {            
            OrderUpdated?.Invoke(this, cryptoOrder);
            return new CryptoResponse<CryptoOrder>(cryptoOrder);
        }

        public async Task<CryptoResponse<Ticker>> GetTickerAsync(string market)
        {
            var tickerResponse = await _bittrexClient.GetTickerAsync(market);
            if(tickerResponse.Success)
                return new CryptoResponse<Ticker>(new Ticker
                {
                    Last = tickerResponse.Data.Last,
                    Ask = tickerResponse.Data.Ask,
                    Bid = tickerResponse.Data.Bid
                });
            return new CryptoResponse<Ticker>(tickerResponse.Error.Message);
        }

        public virtual Task<CryptoResponse<CryptoOrder>> SellCoinAsync(CryptoOrder sellOrder)
        {
            throw new NotImplementedException();
        }

        protected void OnMarketsUpdate(List<BittrexStreamMarketSummary> markets)
        {
            MarketsUpdated?.Invoke(this, markets.Select(m => new Ticker
            {
                Last = m.Last.GetValueOrDefault(),
                Ask = m.Ask,
                Bid = m.Bid,
                Market = m.MarketName,
                BaseVolume = m.BaseVolume.GetValueOrDefault()
            }).ToList());
        }

        protected void OnOrderUpdate(BittrexStreamOrderData bittrexOrder)
        {
            OrderUpdated?.Invoke(this, bittrexOrder.ToCryptoOrder());
        }
        protected void OnOrderUpdate(CryptoOrder cryptoOrder)
        {
            OrderUpdated?.Invoke(this, cryptoOrder);
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
