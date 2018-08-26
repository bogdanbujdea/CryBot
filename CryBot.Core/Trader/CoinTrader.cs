using CryBot.Core.Storage;
using CryBot.Core.Exchange;
using CryBot.Core.Strategies;
using CryBot.Core.Infrastructure;
using CryBot.Core.Exchange.Models;

using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Reactive.Subjects;
using System.Collections.Generic;

namespace CryBot.Core.Trader
{
    public class CoinTrader : ICoinTrader
    {
        private readonly ICryptoApi _cryptoApi;
        private readonly TaskCompletionSource<Budget> _taskCompletionSource;

        public CoinTrader(ICryptoApi cryptoApi)
        {
            _cryptoApi = cryptoApi;
            _taskCompletionSource = new TaskCompletionSource<Budget>();
            PriceUpdated = new Subject<Ticker>();
            OrderUpdated = new Subject<CryptoOrder>();
            TradeUpdated = new Subject<Trade>();
        }

        public void Initialize(TraderState traderState)
        {
            Market = traderState.Market;
            Strategy = new HoldUntilPriceDropsStrategy();

            _cryptoApi.TickerUpdated
                .Where(t => t.Market == traderState.Market)
                .Select(ticker => Observable.FromAsync(token => UpdatePrice(ticker)))
                .Concat()
                .Subscribe(unit => { }, OnCompleted);

            _cryptoApi.OrderUpdated
                .Where(o => o.Market == traderState.Market)
                .Select(order => Observable.FromAsync(token => UpdateOrder(order)))
                .Concat()
                .Subscribe();

            TraderState = traderState;

            Strategy.Settings = traderState.Settings ?? TraderSettings.Default;
            if (TraderState.Trades.Count == 0)
            {
                TraderState.Trades.Add(new Trade { Status = TradeStatus.Empty });
            }
        }

        public string Market { get; set; }

        public Ticker Ticker { get; set; }

        public ITradingStrategy Strategy { get; set; }

        public async Task<Unit> UpdatePrice(Ticker ticker)
        {
            if (IsInTestMode)
            {
                (_cryptoApi as FakeBittrexApi)?.UpdateBuyOrders(Ticker);
            }

            Ticker = ticker;
            if (ticker.Timestamp == default)
                ticker.Timestamp = DateTime.UtcNow;

            await UpdateTrades();

            if (IsInTestMode)
            {
                (_cryptoApi as FakeBittrexApi)?.UpdateSellOrders(Ticker);
            }
            PriceUpdated.OnNext(ticker);
            return Unit.Default;
        }

        public bool IsInTestMode { get; set; }

        public TraderState TraderState { get; set; }

        public async Task<Unit> UpdateOrder(CryptoOrder cryptoOrder)
        {
            try
            {
                Log($"Closed order {cryptoOrder.Uuid} as {cryptoOrder.OrderType} at {cryptoOrder.Limit}");
                if (cryptoOrder.IsClosed == false)
                    return await Task.FromResult(Unit.Default);
                switch (cryptoOrder.OrderType)
                {
                    case CryptoOrderType.LimitSell:
                        TraderState.Budget.Available += cryptoOrder.Price;
                        var tradeForSellOrder = TraderState.Trades.FirstOrDefault(t => t.SellOrder.Uuid == cryptoOrder.Uuid);
                        if (tradeForSellOrder != null)
                        {
                            if (cryptoOrder.Canceled)
                            {
                                tradeForSellOrder.Status = TradeStatus.Bought;
                                tradeForSellOrder.SellOrder.IsClosed = true;
                                return await Task.FromResult(Unit.Default);
                            }
                            var tradeProfit = tradeForSellOrder.BuyOrder.Price.GetReadablePercentageChange(tradeForSellOrder.SellOrder.Price);
                            TraderState.Budget.Profit += tradeProfit;
                            TraderState.Budget.Earned += tradeForSellOrder.SellOrder.Price - tradeForSellOrder.BuyOrder.Price;
                            Log($"{cryptoOrder.Uuid}: SELL - {tradeProfit}");
                            tradeForSellOrder.Profit = tradeProfit;
                            tradeForSellOrder.Status = TradeStatus.Completed;
                            tradeForSellOrder.SellOrder = cryptoOrder;
                        }
                        break;
                    case CryptoOrderType.LimitBuy:
                        var tradeForBuyOrder = TraderState.Trades.FirstOrDefault(t => t.BuyOrder.Uuid == cryptoOrder.Uuid);
                        if (tradeForBuyOrder != null)
                        {
                            if (cryptoOrder.Canceled)
                            {
                                TraderState.Trades.Remove(tradeForBuyOrder);
                                return await Task.FromResult(Unit.Default);
                            }
                            tradeForBuyOrder.Status = TradeStatus.Bought;
                            tradeForBuyOrder.BuyOrder = cryptoOrder;
                        }
                        break;
                }

            }
            finally
            {
                OrderUpdated.OnNext(cryptoOrder);
            }
            return await Task.FromResult(Unit.Default);
        }

        public Task<Budget> FinishTest()
        {
            return _taskCompletionSource.Task;
        }

        public ISubject<Ticker> PriceUpdated { get; }

        public ISubject<CryptoOrder> OrderUpdated { get; }

        public ISubject<Trade> TradeUpdated { get; }

        private void OnCompleted()
        {
            TraderState.Budget.Profit = TraderState.Trades.Sum(t => t.Profit);
            foreach (var trade in TraderState.Trades)
            {
                Log($"{TraderState.Trades.IndexOf(trade)}\t{trade.Status}\t{trade.Profit}");
            }
            Log($"Profit: {TraderState.Budget.Profit}");
            Log($"Available: {TraderState.Budget.Available}");
            Log($"Invested: {TraderState.Budget.Invested}");
            Log($"Earned: {TraderState.Budget.Earned}");
            PriceUpdated.OnCompleted();
            _taskCompletionSource.SetResult(TraderState.Budget);
        }

        private async Task UpdateTrades()
        {
            List<Trade> newTrades = new List<Trade>();

            AddNewTradeIfNecessary();
            var openedTrades = TraderState.Trades.Where(t => t.Status != TradeStatus.Completed).ToList();
            for (var index = 0; index < openedTrades.Count; index++)
            {
                var trade = openedTrades[index];
                var newTrade = await UpdateTrade(trade);
                if (newTrade != Trade.Empty)
                {
                    newTrades.Add(newTrade);
                }
            }

            if (newTrades.Count > 0)
            {
                var distinctTrades = newTrades.GroupBy(t => t.BuyOrder.PricePerUnit).Select(g => g.FirstOrDefault());
                TraderState.Trades.AddRange(distinctTrades);
            }

            var canceledTrades = TraderState.Trades.Where(t => t.Status == TradeStatus.Canceled).ToList();
            foreach (var canceledTrade in canceledTrades)
            {
                TraderState.Trades.Remove(canceledTrade);
            }
        }

        private void AddNewTradeIfNecessary()
        {
            if (TraderState.Trades.Count == 0 || TraderState.Trades.Count(t => t.Status != TradeStatus.Completed) == 0)
            {
                Log($"Adding new trade for {Market}");
                TraderState.Trades.Add(new Trade {Status = TradeStatus.Empty});
            }
        }

        private async Task<Trade> UpdateTrade(Trade trade)
        {
            var tradeAction = Strategy.CalculateTradeAction(Ticker, trade);
            switch (tradeAction.TradeAdvice)
            {
                case TradeAdvice.Buy:
                    var buyOrder = await CreateBuyOrder(tradeAction.OrderPricePerUnit);
                    if (tradeAction.Reason == TradeReason.BuyTrigger)
                    {
                        Log($"Buy trigger at {tradeAction.OrderPricePerUnit}");
                        return new Trade { BuyOrder = buyOrder, Status = TradeStatus.Buying };
                    }
                    trade.BuyOrder = buyOrder;
                    trade.Status = TradeStatus.Buying;
                    break;
                case TradeAdvice.Sell:
                    await CreateSellOrder(trade, tradeAction.OrderPricePerUnit);
                    return Trade.Empty;
                case TradeAdvice.Cancel:
                    Log($"{trade.BuyOrder.Uuid}: Canceling order {trade.BuyOrder.Uuid}");
                    var cancelResponse = await _cryptoApi.CancelOrder(trade.BuyOrder.Uuid);
                    if (cancelResponse.IsSuccessful)
                    {
                        TraderState.Budget.Available += trade.BuyOrder.Price;
                        trade.Status = TradeStatus.Canceled;
                    }
                    break;
            }
            TradeUpdated.OnNext(trade);
            return Trade.Empty;
        }

        private async Task CreateSellOrder(Trade trade, decimal pricePerUnit)
        {
            var sellOrder = new CryptoOrder
            {
                PricePerUnit = pricePerUnit,
                Limit = pricePerUnit,
                OrderType = CryptoOrderType.LimitSell,
                Market = Market,
                Price = pricePerUnit * trade.BuyOrder.Quantity,
                Quantity = trade.BuyOrder.Quantity,
                Uuid = $"{trade.BuyOrder.Uuid}-{Ticker.Id}"
            };
            var sellResponse = await _cryptoApi.SellCoinAsync(sellOrder);
            if (sellResponse.IsSuccessful)
            {
                trade.Status = TradeStatus.Selling;
                trade.SellOrder = sellResponse.Content;
            }
        }

        private async Task<CryptoOrder> CreateBuyOrder(decimal pricePerUnit)
        {
            if (TraderState.Budget.Available < Strategy.Settings.TradingBudget)
            {
                TraderState.Budget.Available += Strategy.Settings.TradingBudget;
                TraderState.Budget.Invested += Strategy.Settings.TradingBudget;
            }
            var priceWithoutCommission = Strategy.Settings.TradingBudget * Consts.BittrexCommission;
            var quantity = priceWithoutCommission / pricePerUnit;
            quantity = quantity.RoundSatoshi();
            var buyOrder = new CryptoOrder
            {
                PricePerUnit = pricePerUnit,
                Price = Strategy.Settings.TradingBudget,
                Quantity = quantity,
                IsClosed = false,
                Market = Market,
                Limit = pricePerUnit,
                Opened = Ticker.Timestamp,
                OrderType = CryptoOrderType.LimitBuy,
                Uuid = $"{Ticker.Id}-{Guid.NewGuid().ToString().Split('-')[0]}"
            };
            var buyResponse = await _cryptoApi.BuyCoinAsync(buyOrder);
            if (buyResponse.IsSuccessful)
            {
                TraderState.Budget.Available -= buyResponse.Content.Price;
            }
            else
            {
                throw new Exception(buyResponse.ErrorMessage);
            }

            return buyResponse.Content;
        }

        private void Log(string text)
        {
            Console.WriteLine(text);
        }
    }
}
