using Bittrex.Net.Objects;
using Bittrex.Net.Interfaces;

using CryBot.Core.Exchange.Models;

using Newtonsoft.Json;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Core.Exchange
{
    public class FakeBittrexApi : BittrexApi
    {
        private readonly List<CryptoOrder> _pendingBuyOrders = new List<CryptoOrder>();
        private readonly List<CryptoOrder> _pendingSellOrders = new List<CryptoOrder>();

        public FakeBittrexApi(IBittrexClient bittrexClient) : base(bittrexClient)
        {
        }

        public FakeBittrexApi(string apiKey, string apiSecret) : base(null)
        {
            Initialize(apiKey, apiSecret, true);
        }

        public List<Candle> Candles { get; set; }

        public override Task<CryptoResponse<Ticker>> GetTickerAsync(string market)
        {
            return Task.FromResult(new CryptoResponse<Ticker>(new Ticker
            {
                Ask = Candles[0].High,
                Bid = Candles[0].Low,
                Timestamp = Candles[0].Timestamp
            }));
        }

        public override async Task<CryptoResponse<List<Candle>>> GetCandlesAsync(string market, TickInterval interval)
        {
            try
            {
                var candlesDirectory = Directory.CreateDirectory("candles");
                var fileName = $"{market}-{interval}.json";
                var filePath = Path.Combine(candlesDirectory.FullName, fileName);
                if (File.Exists(filePath) == false)
                {
                    var candleResponse = await base.GetCandlesAsync(market, interval);
                    File.WriteAllText(filePath, JsonConvert.SerializeObject(candleResponse.Content));
                    Candles = candleResponse.Content;
                }
                else
                {
                    var candlesJson = File.ReadAllText(filePath);
                    Candles = JsonConvert.DeserializeObject<List<Candle>>(candlesJson);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return new CryptoResponse<List<Candle>>(Candles);
        }

        public override async Task SendMarketUpdates(string market)
        {
            if (IsInTestMode)
            {
                if (Candles == null || Candles[0].Interval != TickInterval.OneMinute)
                {
                    await GetCandlesAsync(market, TickInterval.OneMinute);
                }
                //Candles = Candles.Take(5000).ToList();
                foreach (var candle in Candles)
                {
                    try
                    {
                        var ticker = new Ticker
                        {
                            Id = Candles.IndexOf(candle),
                            Market = market,
                            Bid = candle.Low,
                            Ask = candle.High,
                            Timestamp = candle.Timestamp
                        };
                        TickerUpdated.OnNext(ticker);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                TickerUpdated.OnCompleted();
            }
        }

        public override Task<CryptoResponse<CryptoOrder>> BuyCoinAsync(CryptoOrder cryptoOrder)
        {
            cryptoOrder.IsClosed = false;
            _pendingBuyOrders.Add(cryptoOrder);
            return Task.FromResult(new CryptoResponse<CryptoOrder>(cryptoOrder));
        }

        public override Task<CryptoResponse<CryptoOrder>> SellCoinAsync(CryptoOrder sellOrder)
        {
            sellOrder.IsClosed = false;
            _pendingSellOrders.Add(sellOrder);
            return Task.FromResult(new CryptoResponse<CryptoOrder>(sellOrder));
        }

        public override Task<CryptoResponse<CryptoOrder>> CancelOrder(string orderId)
        {
            var existingOrder = _pendingBuyOrders.FirstOrDefault(b => b.Uuid == orderId);
            if (existingOrder != null)
            {
                existingOrder.IsClosed = true;
                _pendingBuyOrders.Remove(existingOrder);
            }
            return Task.FromResult(new CryptoResponse<CryptoOrder>(existingOrder));
        }

        public void UpdateSellOrders(Ticker ticker)
        {
            List<CryptoOrder> removedOrders = new List<CryptoOrder>();
            foreach (var sellOrder in _pendingSellOrders.Where(s => s.IsClosed == false))
            {
                if (ticker.Bid >= sellOrder.PricePerUnit || sellOrder.OrderType == CryptoOrderType.ImmediateSell)
                {
                    sellOrder.IsClosed = true;
                    sellOrder.IsClosed = true;
                    sellOrder.Closed = ticker.Timestamp;
                    sellOrder.OrderType = CryptoOrderType.LimitSell;
                    removedOrders.Add(sellOrder);
                    OrderUpdated.OnNext(sellOrder);
                }
            }

            foreach (var removedOrder in removedOrders)
            {
                _pendingSellOrders.Remove(removedOrder);
            }
        }

        public void UpdateBuyOrders(Ticker ticker)
        {
            List<CryptoOrder> removedOrders = new List<CryptoOrder>();
            foreach (var buyOrder in _pendingBuyOrders)
            {
                if (ticker.Ask <= buyOrder.PricePerUnit)
                {
                    //Console.WriteLine($"Closed buy order {buyOrder.Uuid}");
                    buyOrder.Closed = ticker.Timestamp;
                    buyOrder.IsClosed = true;
                    buyOrder.IsClosed = true;
                    OrderUpdated.OnNext(buyOrder);
                    removedOrders.Add(buyOrder);
                }
                if (buyOrder.Canceled)
                    removedOrders.Add(buyOrder);
            }

            foreach (var removedOrder in removedOrders)
            {
                _pendingBuyOrders.Remove(removedOrder);
            }
        }
    }
}
