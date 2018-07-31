using CryBot.Contracts;
using CryBot.Core.Utilities;

using Orleans;

using System;

using System.Linq;
using System.Timers;
using Timer = System.Timers.Timer;

using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Core.Services
{
    public class CryptoTrader : ICryptoTrader
    {
        private readonly ICryptoApi _cryptoApi;
        private readonly IClusterClient _orleansClient;
        private readonly IHubNotifier _hubNotifier;
        private ITraderGrain _traderGrain;
        private readonly Queue<Ticker> _queue = new Queue<Ticker>();
        private Timer _marketUpdatesTimer;

        public CryptoTrader(ICryptoApi cryptoApi, IClusterClient orleansClient, IHubNotifier hubNotifier)
        {
            _cryptoApi = cryptoApi;
            _orleansClient = orleansClient;
            _hubNotifier = hubNotifier;
            Trades = new List<Trade>();
        }

        public string Market { get; private set; }

        public Ticker Ticker { get; private set; }

        public List<Trade> Trades { get; private set; }

        public TraderSettings Settings { get; private set; }

        public decimal InvestedBTC { get; set; }

        public decimal AvailableBudget { get; set; }

        public decimal CurrentBudget
        {
            get { return AvailableBudget + Trades.Where(t => t.IsActive).Sum(t => t.CurrentValue); }
        }

        public event EventHandler<Ticker> PriceUpdated;

        public event EventHandler TraderUpdated;

        public async Task StartAsync(string market)
        {
            Console.WriteLine($"Starting market {market}");
            _traderGrain = _orleansClient.GetGrain<ITraderGrain>(market);
            Market = market;
            await _traderGrain.SetMarketAsync(market);
            Trades = await _traderGrain.GetActiveTrades();
            var tickerResponse = await _cryptoApi.GetTickerAsync(Market);
            await _traderGrain.UpdatePriceAsync(tickerResponse.Content);
            Ticker = tickerResponse.Content;
            Settings = await _traderGrain.GetSettings();
            _cryptoApi.MarketsUpdated += MarketsUpdated;
            _cryptoApi.OrderUpdated += OrderUpdated;
            _marketUpdatesTimer = new Timer
            {
                Interval = 500,
                Enabled = true
            };
            _marketUpdatesTimer.Elapsed += ProcessQueue;
            _marketUpdatesTimer.Start();
            if (Trades.Count > 0)
            {
                return;
            }

            await CreateBuyOrder(tickerResponse.Content.Ask);
            await CreateBuyOrder(tickerResponse.Content.Bid * Settings.BuyLowerPercentage.ToPercentageMultiplier());
        }

        public async Task UpdatePrice(Ticker ticker)
        {
            Ticker = ticker;
            await ProcessMarketUpdate(ticker);
        }

        public async Task ProcessMarketUpdates()
        {
            _marketUpdatesTimer.Enabled = false;
            _marketUpdatesTimer.Stop();
            while (_queue.Count > 0)
            {
                await ProcessMarketUpdate(_queue.Dequeue());
            }
        }

        protected virtual async void OnPriceUpdated(Ticker e)
        {
            PriceUpdated?.Invoke(this, e);
            if (_hubNotifier != null && _traderGrain != null)
            {
                await _hubNotifier.UpdateTicker(e);
                await _traderGrain.UpdatePriceAsync(e);
            }
        }

        protected virtual async void OnTraderUpdated()
        {
            TraderUpdated?.Invoke(this, EventArgs.Empty);
            if (_hubNotifier != null && _traderGrain != null)
            {
                await _traderGrain.UpdateTrades(Trades);
                var traderData = await _traderGrain.GetTraderData();
                await _hubNotifier.UpdateTrader(traderData);
            }
        }

        private async void ProcessQueue(object state, ElapsedEventArgs elapsedEventArgs)
        {
            if (_queue.Count <= 0)
                return;
            var currentMarket = _queue.Dequeue();
            if (currentMarket == null)
                return;
            await ProcessMarketUpdate(currentMarket);
        }

        private async Task ProcessMarketUpdate(Ticker currentTicker)
        {
            try
            {
                OnPriceUpdated(currentTicker);
                Ticker = currentTicker;
                await UpdateTrades();
                OnTraderUpdated();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async Task CreateBuyOrder(decimal pricePerUnit)
        {
            var trade = new Trade();
            if (AvailableBudget <= InvestedBTC)
            {
                InvestedBTC += Settings.DefaultBudget;
                AvailableBudget += Settings.DefaultBudget;
            }
            var buyResponse = await _cryptoApi.BuyCoinAsync(new CryptoOrder
            {
                PricePerUnit = pricePerUnit,
                Price = Settings.DefaultBudget,
                Quantity = Settings.DefaultBudget / pricePerUnit,
                Market = Market,
                OrderType = CryptoOrderType.LimitBuy
            });
            Console.WriteLine($"Created buy order with price {pricePerUnit}");
            trade.BuyOrder = buyResponse.Content;
            trade.IsActive = false;
            trade.Market = Market;
            Trades.Add(trade);
            await _traderGrain.AddTradeAsync(trade);
        }

        private async void OrderUpdated(object sender, CryptoOrder e)
        {
            if (Trades.Count == 0 || e.Market != Market)
                return;

            if (e.OrderType == CryptoOrderType.LimitSell)
            {
                var tradeForOrder = Trades.FirstOrDefault(t => t.SellOrder.Uuid == e.Uuid);
                if (tradeForOrder != null)
                {
                    Console.WriteLine("Closed sell order");
                    tradeForOrder.IsActive = false;
                    await CreateBuyOrder(e.PricePerUnit * Settings.BuyLowerPercentage.ToPercentageMultiplier());
                }
            }
            else
            {
                var tradeForOrder = Trades.FirstOrDefault(t => t.BuyOrder.Uuid == e.Uuid);
                if (tradeForOrder != null)
                {
                    Console.WriteLine("Closed buy order");
                    tradeForOrder.IsActive = true;
                    tradeForOrder.BuyOrder = e;
                }
            }

            await _traderGrain.UpdateTrades(Trades);
            var traderData = await _traderGrain.GetTraderData();
            await _hubNotifier.UpdateTrader(traderData);
        }

        private void MarketsUpdated(object sender, List<Ticker> e)
        {
            var currentMarket = e.FirstOrDefault(m => m.Market == Market);
            if (currentMarket == null)
                return;
            if (_queue.Count > 0)
            {
                if (_queue.Peek().Bid == currentMarket.Bid)
                    return;

                if (_queue.Peek().Bid == Ticker.Bid)
                    return;
            }
            else
            {
                if (currentMarket.Bid == Ticker.Bid)
                    return;
            }
            _queue.Enqueue(currentMarket);
        }

        private async Task UpdateTrades()
        {
            foreach (var trade in Trades.Where(t => t.IsActive))
            {
                trade.Strategy = new HoldUntilPriceDropsStrategy { Settings = Settings, CurrentTrade = trade };
                var tradeAction = trade.CalculateAction(Ticker);
                var profit = trade.BuyOrder.PricePerUnit.GetReadablePercentageChange(Ticker.Bid, true);

                if (trade.MaxPricePerUnit < Ticker.Bid)
                {
                    Console.WriteLine($"New max for {Market}: {Ticker.Bid}");
                    trade.MaxPricePerUnit = Ticker.Bid;
                }

                trade.Profit = profit;
                Console.WriteLine($"{Market}: {profit}\t{trade.BuyOrder.PricePerUnit}\t{Ticker.Bid}");
                if (tradeAction.TradeAdvice == TradeAdvice.Sell)
                {
                    await CreateSellOrder(trade);
                }
            }
        }

        private async Task CreateSellOrder(Trade trade)
        {
            Console.WriteLine("Creating sell order");
            var cryptoOrder = new CryptoOrder
            {
                PricePerUnit = Ticker.Bid,
                Price = Ticker.Bid * trade.BuyOrder.Quantity,
                Market = Market,
                Quantity = trade.BuyOrder.Quantity
            };
            var sellOrderResponse = await _cryptoApi.SellCoinAsync(cryptoOrder);
            trade.SellOrder = sellOrderResponse.Content;
        }
    }
}
