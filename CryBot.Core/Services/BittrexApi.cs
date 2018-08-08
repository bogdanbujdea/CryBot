using Bittrex.Net;
using Bittrex.Net.Objects;
using Bittrex.Net.Interfaces;

using CryBot.Core.Models;
using CryBot.Core.Utilities;

using CryptoExchange.Net.Authentication;

using System;

using System.Linq;

using System.Threading.Tasks;
using System.Reactive.Subjects;
using System.Collections.Generic;

namespace CryBot.Core.Services
{
    public class BittrexApi : ICryptoApi
    {
        private IBittrexClient _bittrexClient;
        private BittrexSocketClient _bittrexSocketClient;
        private List<Candle> _candles;

        public BittrexApi(IBittrexClient bittrexClient)
        {
            _bittrexClient = bittrexClient;
            TickerUpdated = new Subject<Ticker>();
            OrderUpdated = new Subject<CryptoOrder>();
        }

        public ISubject<Ticker> TickerUpdated { get; private set; }

        public ISubject<CryptoOrder> OrderUpdated { get; private set; }

        public bool IsInTestMode { get; set; }

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

            //_bittrexSocketClient.SubscribeToMarketSummariesUpdate(OnMarketsUpdate);
            //_bittrexSocketClient.SubscribeToOrderUpdates(OnOrderUpdate);
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
            OrderUpdated.OnNext(cryptoOrder);
            return new CryptoResponse<CryptoOrder>(cryptoOrder);
        }

        public async Task<CryptoResponse<Ticker>> GetTickerAsync(string market)
        {
            if (IsInTestMode)
            {
                return new CryptoResponse<Ticker>(new Ticker
                {
                    Ask = _candles[0].High,
                    Bid = _candles[0].Low,
                    Timestamp = _candles[0].Timestamp
                });
            }
            var tickerResponse = await _bittrexClient.GetTickerAsync(market);
            if (tickerResponse.Success)
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

        public async Task<CryptoResponse<List<Market>>> GetMarketsAsync()
        {
            var marketsCallResult = await _bittrexClient.GetMarketsAsync();
            return new CryptoResponse<List<Market>>(marketsCallResult.Data.Select(m => new Market
            {
                Name = m.MarketName
            }).ToList());
        }

        public async Task<CryptoResponse<List<Candle>>> GetCandlesAsync(string market, TickInterval interval)
        {
            var callResult = await _bittrexClient.GetCandlesAsync(market, interval);
            var candles = callResult.Data.Select(c => new Candle
            {
                Timestamp = c.Timestamp,
                Currency = market,
                Low = c.Low,
                High = c.High,
                Open = c.Open,
                Close = c.Close,
                Volume = c.BaseVolume
            }).ToList();
            _candles = candles.Take(candles.Count).ToList();
            return new CryptoResponse<List<Candle>>(candles);
        }

        public async Task SendMarketUpdates(string market)
        {
            if (IsInTestMode)
            {
                var oldPercentage = -1;
                foreach (var candle in _candles)
                {
                    try
                    {
                        var percentage = _candles.IndexOf(candle) * 100 / _candles.Count;
                        if (percentage != oldPercentage)
                        {
                            Console.WriteLine($"{percentage}%");
                            oldPercentage = percentage;
                        }
                        //Console.WriteLine($"BAPI: {_candles.IndexOf(candle) + 1}\t{candle.Low}\t{candle.High}");
                        TickerUpdated.OnNext(new Ticker
                        {
                            Id = _candles.IndexOf(candle),
                            Market = market,
                            Bid = candle.Low,
                            Ask = candle.High,
                            Timestamp = candle.Timestamp
                        });
                        await Task.Delay(2000);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
        }

        public virtual Task<CryptoResponse<CryptoOrder>> CancelOrder(string orderId)
        {
            throw new NotImplementedException();
        }

        protected void OnMarketsUpdate(List<BittrexStreamMarketSummary> markets)
        {
            var tickers = markets.Select(m => new Ticker
            {
                Last = m.Last.GetValueOrDefault(),
                Ask = m.Ask,
                Bid = m.Bid,
                Market = m.MarketName,
                BaseVolume = m.BaseVolume.GetValueOrDefault()
            });

            foreach (var ticker in tickers)
            {
                TickerUpdated.OnNext(ticker);
            }
        }

        protected void OnOrderUpdate(BittrexStreamOrderData bittrexOrder)
        {
            OrderUpdated.OnNext(bittrexOrder.ToCryptoOrder());
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
