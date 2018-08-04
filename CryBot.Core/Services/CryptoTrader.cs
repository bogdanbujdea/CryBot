using CryBot.Core.Models;
using CryBot.Core.Utilities;

using Orleans;

using System;

using System.Linq;
using System.Timers;
using System.Threading;

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
        private decimal _profitBTC = 0M;
        private static readonly SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);
        private int _tickersCount = 0;

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
            AvailableBudget = InvestedBTC = Settings.TradingBudget;
            Settings.BuyLowerPercentage = -1;
            Settings.MinimumTakeProfit = 0M;
            Settings.HighStopLossPercentage = -0.1M;
            Settings.StopLoss = -4;
            Settings.BuyTrigger = -2;
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

            var trade = await CreateBuyOrder(tickerResponse.Content.Bid);
            Trades.Add(trade);
            //await CreateBuyOrder(tickerResponse.Content.Bid * Settings.BuyLowerPercentage.ToPercentageMultiplier());
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

        public static int LastProcessedTicker { get; set; }

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
            try
            {
                if (_queue.Count <= 0)
                    return;
                var currentMarket = _queue.Dequeue();
                if (currentMarket == null)
                    return;
                await ProcessMarketUpdate(currentMarket);
            }
            catch (Exception e)
            {
            }
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

        private async Task<Trade> CreateBuyOrder(decimal pricePerUnit)
        {
            var trade = new Trade();
            if (AvailableBudget < Settings.TradingBudget)
            {
                InvestedBTC += Settings.TradingBudget;
                AvailableBudget = Settings.TradingBudget;
            }
            var buyResponse = await _cryptoApi.BuyCoinAsync(new CryptoOrder
            {
                PricePerUnit = pricePerUnit,
                Price = AvailableBudget,
                Quantity = AvailableBudget / pricePerUnit,
                Opened = Ticker.Timestamp,
                Market = Market,
                OrderType = CryptoOrderType.LimitBuy
            });
            AvailableBudget = 0;
            trade.BuyOrder = buyResponse.Content;
            trade.IsActive = false;
            trade.Market = Market;
            await _traderGrain.AddTradeAsync(trade);
            return trade;
        }

        private async void OrderUpdated(object sender, CryptoOrder e)
        {
            if (Trades.Count == 0 || e.Market != Market)
                return;
            try
            {
                if (e.OrderType == CryptoOrderType.LimitSell)
                {
                    var tradeForOrder = Trades.FirstOrDefault(t => t.SellOrder.Uuid == e.Uuid);
                    if (tradeForOrder != null)
                    {
                        tradeForOrder.IsActive = false;
                        tradeForOrder.SellOrder.IsClosed = true;
                        tradeForOrder.SellOrder.IsOpened = false;
                        Console.WriteLine($"Available: {AvailableBudget}");
                        Console.WriteLine($"Adding sell price of {e.Price} to {AvailableBudget}");
                        AvailableBudget += Math.Round(e.Price, 8);
                        var trade = await CreateBuyOrder(
                            e.PricePerUnit * Settings.BuyLowerPercentage.ToPercentageMultiplier());
                        trade.BuyOrder.Opened = Ticker.Timestamp;
                        Trades.Add(trade);
                        _profitBTC += e.Price - tradeForOrder.BuyOrder.Price;
                        var profit = InvestedBTC.GetReadablePercentageChange(InvestedBTC + _profitBTC);
                        Console.WriteLine($"Current profit: {profit}%");
                    }
                }
                else
                {
                    var tradeForOrder = Trades.FirstOrDefault(t => t.BuyOrder.Uuid == e.Uuid);
                    if (tradeForOrder != null)
                    {
                        Console.WriteLine($"Closed buy order {e.Uuid} at {Ticker.Timestamp} for {Ticker.Ask}");
                        tradeForOrder.IsActive = true;
                        tradeForOrder.BuyOrder = e;
                    }
                }

                await _traderGrain.UpdateTrades(Trades);
                var traderData = await _traderGrain.GetTraderData();
                await _hubNotifier.UpdateTrader(traderData);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private void MarketsUpdated(object sender, List<Ticker> e)
        {
            var currentMarket = e.FirstOrDefault(m => m.Market == Market);
            if (currentMarket == null)
                return;
            _queue.Enqueue(currentMarket);
        }

        private async Task UpdateTrades()
        {
            Trade newTrade = null;
            await SemaphoreSlim.WaitAsync();
            
            try
            {
                for (var index = 0; index < Trades.Count; index++)
                {
                    var trade = Trades[index];
                    if (trade.IsActive == false && trade.SellOrder.IsClosed)
                        continue;
                    trade.Strategy = new HoldUntilPriceDropsStrategy {Settings = Settings};
                    var tradeAction = trade.CalculateAction(Ticker);

                    //Console.WriteLine($"{Market}:\t\t{trade.Profit}\t\t{Ticker.Bid}\t\t{Ticker.Timestamp:F}");
                    if (trade.SellOrder.IsOpened)
                    {
                        return;
                    }

                    if (tradeAction.TradeAdvice == TradeAdvice.Cancel)
                    {
                        Console.WriteLine($"Canceled order {trade.BuyOrder.Uuid}");
                        trade.IsActive = false;
                        trade.BuyOrder.Canceled = true;
                        trade.Profit = 0;
                        trade.CurrentTicker = new Ticker();
                        newTrade = await CreateBuyOrder(tradeAction.OrderPricePerUnit);
                    }

                    if (tradeAction.TradeAdvice == TradeAdvice.Buy)
                    {
                        Console.WriteLine($"Buying due to {tradeAction.Reason}");
                        newTrade = await CreateBuyOrder(tradeAction.OrderPricePerUnit);
                    }
                    else if (tradeAction.TradeAdvice == TradeAdvice.Sell)
                    {
                        if (tradeAction.Reason == TradeReason.StopLoss)
                        {
                            trade.SellOrder.OrderType = CryptoOrderType.ImmediateSell;
                        }

                        CreateSellOrder(trade);
                        Console.WriteLine(
                            $"SELLING due to {tradeAction.Reason}\t{Ticker.Bid}\t{trade.Profit}%\t{Ticker.Timestamp:F}\t{trade.SellOrder.Price} BTC\t\t{trade.BuyOrder.Uuid}");
                    }
                }

                if (newTrade != null) Trades.Add(newTrade);
                var canceledTrades = Trades.Where(t => t.BuyOrder.Canceled).ToList();
                foreach (var canceledTrade in canceledTrades)
                {
                    Trades.Remove(canceledTrade);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                //When the task is ready, release the semaphore. It is vital to ALWAYS release the semaphore when we are ready, or else we will end up with a Semaphore that is forever locked.
                //This is why it is important to do the Release within a try...finally clause; program execution may crash or take a different path, this way you are guaranteed execution
                SemaphoreSlim.Release();
                Console.WriteLine($"CTRADER: {_tickersCount++}");
                LastProcessedTicker = _tickersCount;
            }
        }

        private void CreateSellOrder(Trade trade)
        {
            var cryptoOrder = new CryptoOrder
            {
                PricePerUnit = Ticker.Bid,
                Price = Ticker.Bid * trade.BuyOrder.Quantity,
                Market = Market,
                Opened = Ticker.Timestamp,
                Quantity = trade.BuyOrder.Quantity,
                OrderType = CryptoOrderType.LimitSell
            };
            //var sellOrderResponse = await _cryptoApi.SellCoinAsync(cryptoOrder);
            cryptoOrder.Uuid = "SELLORDER-" + FakeBittrexApi.SellOrdersCount++;
            trade.SellOrder = cryptoOrder;
            trade.SellOrder.Closed = Ticker.Timestamp;
            trade.SellOrder.IsOpened = true;
            OrderUpdated(this, cryptoOrder);
        }
    }
}
