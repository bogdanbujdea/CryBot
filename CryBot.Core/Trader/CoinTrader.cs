using CryBot.Core.Storage;
using CryBot.Core.Strategies;
using CryBot.Core.Notifications;
using CryBot.Core.Exchange.Models;

using Orleans;

using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Core.Trader
{
    public class CoinTrader
    {
        private readonly IClusterClient _orleansClient;
        private readonly IHubNotifier _hubNotifier;
        private readonly IPushManager _pushManager;
        private readonly ICryptoBroker _cryptoBroker;
        private ITraderGrain _traderGrain;
        private readonly TaskCompletionSource<Budget> _taskCompletionSource;

        public CoinTrader(IClusterClient orleansClient, IHubNotifier hubNotifier, IPushManager pushManager, ICryptoBroker cryptoBroker)
        {
            _orleansClient = orleansClient;
            _hubNotifier = hubNotifier;
            _pushManager = pushManager;
            _cryptoBroker = cryptoBroker;
            _taskCompletionSource = new TaskCompletionSource<Budget>();
        }

        public void Initialize(string market)
        {
            Market = market;
            _cryptoBroker.PriceUpdated
                .Select(ticker => Observable.FromAsync(token => PriceUpdated(ticker)))
                .Concat()
                .Subscribe();
            _cryptoBroker.TradeUpdated
                .Select(ticker => Observable.FromAsync(token => UpdateTrade(ticker)))
                .Concat()
                .Subscribe();
            _cryptoBroker.OrderUpdated
                .Select(ticker => Observable.FromAsync(token => UpdateOrder(ticker)))
                .Concat()
                .Subscribe();
        }

        private async Task<Unit> PriceUpdated(Ticker ticker)
        { 
            if (!IsInTestMode)
            {
                await _traderGrain.UpdateTrades(TraderState.Trades);
                await _hubNotifier.UpdateTicker(ticker);
            }
            return Unit.Default;
        }

        public TraderState TraderState { get; set; }

        public string Market { get; set; }

        public Ticker Ticker { get; set; }

        public ITradingStrategy Strategy { get; set; }

        public async Task UpdatePrice(Ticker ticker)
        {
            await _cryptoBroker.UpdatePrice(ticker);
        }

        public bool IsInTestMode { get; set; }

        public async Task StartAsync()
        {
            Strategy = new HoldUntilPriceDropsStrategy();
            _traderGrain = _orleansClient.GetGrain<ITraderGrain>(Market);
            await _traderGrain.UpdateTrades(new List<Trade>());
            await _traderGrain.SetMarketAsync(Market);
            TraderState = await _traderGrain.GetTraderData();
            TraderState.Trades = TraderState.Trades ?? new List<Trade>();

            Strategy.Settings = TraderSettings.Default;
            if (TraderState.Trades.Count == 0)
            {
                TraderState.Trades.Add(new Trade { Status = TradeStatus.Empty });
            }
            _cryptoBroker.Initialize(TraderState);
        }

        public async Task<Unit> UpdateOrder(CryptoOrder cryptoOrder)
        {
            if (!IsInTestMode)
            {
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

        private async void OnCompleted()
        {
            TraderState.Budget.Profit = TraderState.Trades.Sum(t => t.Profit);
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
            await _hubNotifier.UpdateTrader(await _traderGrain.GetTraderData());
            _taskCompletionSource.SetResult(TraderState.Budget);
        }

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
