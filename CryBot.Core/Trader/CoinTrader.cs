using CryBot.Core.Exchange;
using CryBot.Core.Exchange.Models;
using CryBot.Core.Infrastructure;
using CryBot.Core.Storage;
using CryBot.Core.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

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
                            tradeForSellOrder.SellOrder = cryptoOrder;
                            var tradeProfit = tradeForSellOrder.BuyOrder.Price.GetReadablePercentageChange(tradeForSellOrder.SellOrder.Price);

                            Console.WriteLine($"Sold {tradeForSellOrder.BuyOrder.Uuid} with profit {tradeProfit} \t\t" +
                                              $" {tradeForSellOrder.BuyReason}: {tradeForSellOrder.BuyOrder.PricePerUnit} " +
                                              $"\t\t {tradeForSellOrder.SellReason}: {tradeForSellOrder.SellOrder.PricePerUnit}");


                            TraderState.Budget.Profit += tradeProfit;
                            TraderState.Budget.Earned += tradeForSellOrder.SellOrder.Price - tradeForSellOrder.BuyOrder.Price;
                            tradeForSellOrder.Profit = tradeProfit;
                            tradeForSellOrder.Status = TradeStatus.Completed;
                            var tradeWithTriggeredBuy =
                                TraderState.Trades.FirstOrDefault(t =>
                                    t.TriggeredBuy && t.Status != TradeStatus.Completed);
                            if (tradeWithTriggeredBuy != null
                                && TraderState.Trades.Count(t => t.TriggeredBuy && t.Status != TradeStatus.Completed) == 0)
                            {
                                //Console.WriteLine($"Set triggered buy to false for {tradeWithTriggeredBuy.BuyOrder.Uuid}");
                                tradeWithTriggeredBuy.TriggeredBuy = false;
                            }
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

        public List<Candle> Candles { get; set; }

        private void OnCompleted()
        {
            TraderState.Budget.Profit = TraderState.Trades.Sum(t => t.Profit);
            foreach (var trade in TraderState.Trades.OrderByDescending(t => t.Profit))
            {
                Log($"{TraderState.Trades.IndexOf(trade)}" +
                    $"\t{trade.Status}" +
                    $"\t{trade.Profit}" +
                    $"\t{trade.BuyReason}" +
                    $"\t{trade.BuyOrder.PricePerUnit}" +
                    $"\t{trade.SellReason}" +
                    $"\t{trade.SellOrder.PricePerUnit}");
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
                TraderState.Trades.Add(new Trade { Status = TradeStatus.Empty });
            }
        }

        private async Task<Trade> UpdateTrade(Trade trade)
        {
            if (TraderState.Trades.Count > 1)
                Strategy.Settings.FirstBuyLowerPercentage = Strategy.Settings.BuyLowerPercentage;
            var tradeAction = Strategy.CalculateTradeAction(Ticker, trade);
            var emaAdvice = GetEmaAdvice();
            if (emaAdvice == TradeAdvice.Buy && TraderState.Trades.Count(t => t.Status == TradeStatus.Buying) < 2)
            {
                tradeAction.TradeAdvice = TradeAdvice.Buy;
                tradeAction.Reason = TradeReason.EmaBuy;
                tradeAction.OrderPricePerUnit = Ticker.Bid;
            }
            Ticker.LatestEmaAdvice = emaAdvice;

            switch (tradeAction.TradeAdvice)
            {
                case TradeAdvice.Buy:
                    if (emaAdvice == TradeAdvice.Sell)
                        break;
                    if (trade.Status != TradeStatus.Empty)
                        break;
                    var buyOrder = await CreateBuyOrder(tradeAction.OrderPricePerUnit);
                    if (tradeAction.Reason == TradeReason.BuyTrigger)
                    {
                        var newTrade = new Trade { BuyOrder = buyOrder, Status = TradeStatus.Buying, BuyReason = tradeAction.Reason };
                        return newTrade;
                    }
                    trade.BuyReason = tradeAction.Reason;
                    trade.BuyOrder = buyOrder;
                    trade.Status = TradeStatus.Buying;
                    break;
                case TradeAdvice.Sell:
                    if (emaAdvice != TradeAdvice.Sell && tradeAction.Reason == TradeReason.StopLoss)
                        break;
                    if (tradeAction.Reason == TradeReason.TakeProfit && emaAdvice == TradeAdvice.Buy)
                    {
                        return Trade.Empty;
                    }
                    trade.SellReason = tradeAction.Reason;
                    await CreateSellOrder(trade, tradeAction.OrderPricePerUnit);
                    return Trade.Empty;
                case TradeAdvice.Cancel:
                    var cancelResponse = await _cryptoApi.CancelOrder(trade.BuyOrder.Uuid);
                    if (cancelResponse.IsSuccessful)
                    {
                        Strategy.Settings.BuyLowerPercentage = 0;
                        TraderState.Budget.Available += trade.BuyOrder.Price;
                        trade.Status = TradeStatus.Canceled;
                    }
                    break;
            }
            TradeUpdated.OnNext(trade);
            return Trade.Empty;
        }

        private TradeAdvice GetEmaAdvice()
        {
            var hourlyCandlesUntilNow = Candles.TakeWhile(t => t.Timestamp <= Ticker.Timestamp).ToList();
            if (hourlyCandlesUntilNow.Count < 36)
                return TradeAdvice.Hold;
            var emaAdvice = new EmaCross().Forecast(hourlyCandlesUntilNow);
            return emaAdvice;
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
                if (Ticker.Market == "BTC-BCH")
                {
                    Console.WriteLine(TraderState.Budget.Invested);
                }
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
