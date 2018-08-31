using CryBot.Core.Storage;
using CryBot.Core.Exchange;
using CryBot.Core.Strategies;
using CryBot.Core.Notifications;
using CryBot.Core.Exchange.Models;
using CryBot.Core.Trader.Backtesting;

using Orleans;

using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Core.Trader
{
    public class LiveTrader
    {
        private readonly IClusterClient _orleansClient;
        private readonly IHubNotifier _hubNotifier;
        private readonly IPushManager _pushManager;
        private readonly ICoinTrader _coinTrader;
        private readonly IBackTester _backTester;
        private readonly ICryptoApi _cryptoApi;
        private ITraderGrain _traderGrain;
        private readonly TaskCompletionSource<Budget> _taskCompletionSource;

        public LiveTrader(IClusterClient orleansClient, IHubNotifier hubNotifier, IPushManager pushManager, ICoinTrader coinTrader, IBackTester backTester)
        {
            _orleansClient = orleansClient;
            _hubNotifier = hubNotifier;
            _pushManager = pushManager;
            _coinTrader = coinTrader;
            _backTester = backTester;
            _taskCompletionSource = new TaskCompletionSource<Budget>();
        }

        public void Initialize(string market)
        {
            Market = market;
            _coinTrader.PriceUpdated
                .Select(ticker => Observable.FromAsync(token => PriceUpdated(ticker)))
                .Concat()
                .Subscribe(unit => { }, OnCompleted);
            _coinTrader.TradeUpdated
                .Select(ticker => Observable.FromAsync(token => UpdateTrade(ticker)))
                .Concat()
                .Subscribe();
            _coinTrader.OrderUpdated
                .Select(ticker => Observable.FromAsync(token => UpdateOrder(ticker)))
                .Concat()
                .Subscribe();
        }

        public TraderState TraderState { get; set; }

        public string Market { get; set; }

        public Ticker Ticker { get; set; }

        public ITradingStrategy Strategy { get; set; }

        public bool IsInTestMode { get; set; }

        public async Task StartAsync()
        {
            Console.WriteLine($"Starting {Market}");
            Strategy = new HoldUntilPriceDropsStrategy();
            _traderGrain = _orleansClient.GetGrain<ITraderGrain>(Market);
            await _traderGrain.SetMarketAsync(Market);
            TraderState = await _traderGrain.GetTraderData();
            TraderState.Trades = TraderState.Trades ?? new List<Trade>();

            Strategy.Settings = TraderSettings.Default;
            Console.WriteLine($"Downloaded candles for {Market}");
            var results = await _backTester.FindBestSettings(Market);
            Console.WriteLine($"Best settings for {Market} are {results[0].Settings} with a profit of {results[0].Budget.Profit}%");
            Strategy.Settings = results[0].Settings;
            if (TraderState.Trades.Count == 0)
            {
                TraderState.Trades.Add(new Trade { Status = TradeStatus.Empty });
            }

            _coinTrader.IsInTestMode = IsInTestMode;
            _coinTrader.Initialize(TraderState);
            CanUpdate = true;
            //await UpdateOrders();
        }

        public async Task<Unit> UpdateOrder(CryptoOrder cryptoOrder)
        {
            if (!IsInTestMode)
            {
                await _traderGrain.UpdateTrades(TraderState.Trades);
                var traderData = await _traderGrain.GetTraderData();
                traderData.CurrentTicker = Ticker;
                await _hubNotifier.UpdateTrader(traderData);
            }
            return await Task.FromResult(Unit.Default);
        }

        public Task<Budget> FinishTest()
        {
            return _taskCompletionSource.Task;
        }

        private async Task<Unit> PriceUpdated(Ticker ticker)
        {
            Ticker = ticker;
            if (!IsInTestMode)
            {
                await _traderGrain.UpdateTrades(TraderState.Trades);
                await _traderGrain.UpdatePriceAsync(ticker);
                await _hubNotifier.UpdateTicker(ticker);
            }
            return Unit.Default;
        }

        private async Task UpdateOrders()
        {
            var trades = TraderState.Trades.Where(t => t.Status != TradeStatus.Completed).ToList();
            for (var index = 0; index < trades.Count; index++)
            {
                Trade trade = trades[index];
                if (trade.BuyOrder.Uuid == null)
                    continue;
                var buyOrderResponse = await _cryptoApi.GetOrderInfoAsync(trade.BuyOrder.Uuid);
                if (buyOrderResponse.IsSuccessful)
                {
                    var orderInfo = buyOrderResponse.Content;
                    if (orderInfo.IsClosed != trade.BuyOrder.IsClosed)
                    {
                        await _coinTrader.UpdateOrder(orderInfo);
                    }
                }

                if (trade.BuyOrder.IsClosed && trade.SellOrder.Uuid != null)
                {
                    var sellOrderResponse = await _cryptoApi.GetOrderInfoAsync(trade.SellOrder.Uuid);
                    if (sellOrderResponse.IsSuccessful)
                    {
                        var orderInfo = sellOrderResponse.Content;
                        if (orderInfo.IsClosed != trade.SellOrder.IsClosed)
                        {
                            await _coinTrader.UpdateOrder(orderInfo);
                        }
                    }
                }
            }
        }

        private async void OnCompleted()
        {
            TraderState.Budget.Profit = TraderState.Trades.Sum(t => t.Profit);
            if (CanUpdate)
            {
                foreach (var trade in TraderState.Trades)
                {
                    Console.WriteLine($"{TraderState.Trades.IndexOf(trade)}\t{trade.Status}\t{trade.Profit}");
                }
                Console.WriteLine($"Profit: {TraderState.Budget.Profit}");
                Console.WriteLine($"Available: {TraderState.Budget.Available}");
                Console.WriteLine($"Invested: {TraderState.Budget.Invested}");
                Console.WriteLine($"Earned: {TraderState.Budget.Earned}");
                await _traderGrain.UpdateTrades(TraderState.Trades);
                await _hubNotifier.UpdateTicker(Ticker);
                await _traderGrain.UpdatePriceAsync(Ticker);
                await _traderGrain.SetBudgetAsync(TraderState.Budget);
                await _hubNotifier.UpdateTrader(await _traderGrain.GetTraderData());
            }
            _taskCompletionSource.SetResult(TraderState.Budget);
        }

        public bool CanUpdate { get; set; }

        private async Task<Unit> UpdateTrade(Trade trade)
        {
            switch (trade.Status)
            {
                case TradeStatus.Buying:
                    await _pushManager.TriggerPush(PushMessage.FromMessage($"Got buy signal for {Market}"));
                    break;
                case TradeStatus.Selling:
                    await _pushManager.TriggerPush(PushMessage.FromMessage($"Got sell signal for {Market}"));
                    break;
                case TradeStatus.Canceled:
                    await _pushManager.TriggerPush(PushMessage.FromMessage($"Got cancel signal for {Market}"));
                    break;
            }

            return Unit.Default;
        }
    }
}
