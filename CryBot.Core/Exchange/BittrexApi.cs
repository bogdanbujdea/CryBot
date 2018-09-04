using Bittrex.Net;
using Bittrex.Net.Objects;
using Bittrex.Net.Interfaces;

using CryBot.Core.Infrastructure;
using CryBot.Core.Exchange.Models;

using CryptoExchange.Net.Authentication;

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Subjects;
using System.Collections.Generic;

namespace CryBot.Core.Exchange
{
    public class BittrexApi : ICryptoApi
    {
        private IBittrexClient _bittrexClient;
        private BittrexSocketClient _bittrexSocketClient;

        public BittrexApi(IBittrexClient bittrexClient)
        {
            _bittrexClient = bittrexClient;
            TickerUpdated = new Subject<Ticker>();
            OrderUpdated = new Subject<CryptoOrder>();
        }

        public ISubject<Ticker> TickerUpdated { get; private set; }

        public ISubject<CryptoOrder> OrderUpdated { get; private set; }

        public bool IsInTestMode { get; set; }

        public void Initialize(string apiKey, string apiSecret, bool isInTestMode, bool enableStreaming = false)
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

            if (!isInTestMode && enableStreaming)
            {
                _bittrexSocketClient.SubscribeToMarketSummariesUpdate(OnMarketsUpdate);
                _bittrexSocketClient.SubscribeToOrderUpdates(OnOrderUpdate);
            }
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
            var buyResult = await _bittrexClient.PlaceOrderAsync(OrderSide.Buy, cryptoOrder.Market, cryptoOrder.Quantity, cryptoOrder.Limit);
            if (buyResult.Success)
            {
                cryptoOrder.Uuid = buyResult.Data.Uuid.ToString();
                return new CryptoResponse<CryptoOrder>(cryptoOrder);
            }

            return new CryptoResponse<CryptoOrder>(buyResult.Error.Message);
        }

        public virtual async Task<CryptoResponse<Ticker>> GetTickerAsync(string market)
        {
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

        public virtual async Task<CryptoResponse<CryptoOrder>> SellCoinAsync(CryptoOrder sellOrder)
        {
            var sellResult = await _bittrexClient.PlaceOrderAsync(OrderSide.Sell, sellOrder.Market, sellOrder.Quantity, sellOrder.Limit);
            if (sellResult.Success)
            {
                sellOrder.Uuid = sellResult.Data.Uuid.ToString();
                return new CryptoResponse<CryptoOrder>(sellOrder);
            }
            else
                return new CryptoResponse<CryptoOrder>(sellResult.Error.Message);
        }

        public async Task<CryptoResponse<List<Market>>> GetMarketsAsync()
        {
            var marketsCallResult = await _bittrexClient.GetMarketsAsync();
            return new CryptoResponse<List<Market>>(marketsCallResult.Data.Select(m => new Market
            {
                Name = m.MarketName
            }).ToList());
        }

        public virtual async Task<CryptoResponse<List<Candle>>> GetCandlesAsync(string market, TickInterval interval)
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
                Volume = c.BaseVolume,
                Interval = interval
            }).ToList();
            return new CryptoResponse<List<Candle>>(candles);
        }

        public virtual async Task SendMarketUpdates(string market)
        {
        }

        public virtual async Task<CryptoResponse<CryptoOrder>> CancelOrder(string orderId)
        {
            await _bittrexClient.CancelOrderAsync(Guid.Parse(orderId));
            return new CryptoResponse<CryptoOrder>(new CryptoOrder());
        }

        public async Task<CryptoResponse<CryptoOrder>> GetOrderInfoAsync(string uuid)
        {
            var callResult = await _bittrexClient.GetOrderAsync(Guid.Parse(uuid));
            if (callResult.Success)
            {
                return new CryptoResponse<CryptoOrder>(new CryptoOrder
                {
                    PricePerUnit = callResult.Data.PricePerUnit.GetValueOrDefault(),
                    Limit = callResult.Data.Limit,
                    Price = callResult.Data.Price,
                    Closed = callResult.Data.Closed.GetValueOrDefault(),
                    Quantity = callResult.Data.Quantity,
                    QuantityRemaining = callResult.Data.QuantityRemaining,
                    CommissionPaid = callResult.Data.CommissionPaid,
                    Canceled = callResult.Data.CancelInitiated,
                    Opened = callResult.Data.Opened,
                    Market = callResult.Data.Exchange,
                    IsClosed = !callResult.Data.IsOpen,
                    Uuid = callResult.Data.OrderUuid.ToString(),
                    OrderType = callResult.Data.Type == OrderSideExtended.LimitBuy ? CryptoOrderType.LimitBuy : CryptoOrderType.LimitSell
                });
            }
            return new CryptoResponse<CryptoOrder>(callResult.Error.Message);
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
