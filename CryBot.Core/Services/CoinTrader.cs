using CryBot.Core.Models;
using CryBot.Core.Utilities;
using Orleans;

using System;

using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

using System.Threading.Tasks;

using System.Collections.Generic;

namespace CryBot.Core.Services
{
    public class CoinTrader
    {
        private readonly ICryptoApi _cryptoApi;
        private readonly IClusterClient _orleansClient;
        private readonly IHubNotifier _hubNotifier;
        private readonly IPushManager _pushManager;
        private ITraderGrain _traderGrain;

        public CoinTrader(ICryptoApi cryptoApi, IClusterClient orleansClient, IHubNotifier hubNotifier, IPushManager pushManager)
        {
            _cryptoApi = cryptoApi;
            _orleansClient = orleansClient;
            _hubNotifier = hubNotifier;
            _pushManager = pushManager;
        }

        public void Initialize(string market)
        {
            Market = market;
            Strategy = new HoldUntilPriceDropsStrategy();
            _cryptoApi.TickerUpdated
                .Where(t => t.Market == market)
                .Select(ticker => Observable.FromAsync(token => UpdatePrice(ticker)))
                .Concat()
                .Subscribe();

            _cryptoApi.OrderUpdated
                .Where(o => o.Market == market)
                .Select(order => Observable.FromAsync(token => UpdateOrder(order)))
                .Concat()
                .Subscribe();
        }

        public string Market { get; set; }

        public Ticker Ticker { get; set; }

        public ITradingStrategy Strategy { get; set; }

        public List<Trade> Trades { get; set; } = new List<Trade>();

        public Budget Budget { get; set; } = new Budget();

        public async Task<Unit> UpdatePrice(Ticker ticker)
        {
            if(ticker.Timestamp == default)
                ticker.Timestamp = DateTime.UtcNow;
            Ticker = ticker;
            await UpdateTrades();
            await _traderGrain.UpdateTrades(Trades);
            await _hubNotifier.UpdateTicker(ticker);
            return Unit.Default;
        }

        public async Task StartAsync()
        {
            _traderGrain = _orleansClient.GetGrain<ITraderGrain>(Market);
            await _traderGrain.SetMarketAsync(Market);
            var traderState = await _traderGrain.GetTraderData();
            Trades = traderState.Trades ?? new List<Trade>();

            Strategy.Settings = traderState.Settings ?? TraderSettings.Default;
            if (Trades.Count == 0)
            {
                Trades.Add(new Trade { Status = TradeStatus.Empty });
            }
        }

        public async Task<Unit> UpdateOrder(CryptoOrder cryptoOrder)
        {
            switch (cryptoOrder.OrderType)
            {
                case CryptoOrderType.LimitSell:
                    Budget.Available += cryptoOrder.Price;
                    var tradeForSellOrder = Trades.FirstOrDefault(t => t.SellOrder.Uuid == cryptoOrder.Uuid);
                    if (tradeForSellOrder != null)
                    {
                        if (cryptoOrder.Canceled)
                        {
                            tradeForSellOrder.Status = TradeStatus.Bought;
                            tradeForSellOrder.SellOrder.IsOpened = false;
                            return await Task.FromResult(Unit.Default);
                        }
                        var tradeProfit = tradeForSellOrder.BuyOrder.Price.GetReadablePercentageChange(tradeForSellOrder.SellOrder.Price);
                        Budget.Profit += tradeProfit;
                        Budget.Earned += tradeForSellOrder.SellOrder.Price - tradeForSellOrder.BuyOrder.Price;
                        Console.WriteLine($"{cryptoOrder.Uuid}: SELL - {tradeProfit}");
                        await _pushManager.TriggerPush(PushMessage.FromMessage($"Sold {Market} for profit {tradeProfit}%"));
                        tradeForSellOrder.Profit = tradeProfit;
                        tradeForSellOrder.Status = TradeStatus.Completed;
                        tradeForSellOrder.SellOrder = cryptoOrder;
                    }
                    break;
                case CryptoOrderType.LimitBuy:
                    var tradeForBuyOrder = Trades.FirstOrDefault(t => t.BuyOrder.Uuid == cryptoOrder.Uuid);
                    if (tradeForBuyOrder != null)
                    {
                        await _pushManager.TriggerPush(PushMessage.FromMessage($"Bought {Market} at {cryptoOrder.Limit} BTC"));
                        if (cryptoOrder.Canceled)
                        {
                            tradeForBuyOrder.Status = TradeStatus.Empty;
                            tradeForBuyOrder.BuyOrder = new CryptoOrder();
                            return await Task.FromResult(Unit.Default);
                        }
                        tradeForBuyOrder.Status = TradeStatus.Bought;
                        tradeForBuyOrder.BuyOrder = cryptoOrder;
                    }
                    break;
            }
            
            var traderData = await _traderGrain.GetTraderData();
            traderData.CurrentTicker = Ticker;
            await _hubNotifier.UpdateTrader(traderData);
            return await Task.FromResult(Unit.Default);
        }

        private async Task UpdateTrades()
        {
            List<Trade> newTrades = new List<Trade>();
            foreach (var trade in Trades.Where(t => t.Status != TradeStatus.Completed))
            {
                var newTrade = await UpdateTrade(trade);
                if (newTrade != Trade.Empty)
                {
                    newTrades.Add(newTrade);
                }
            }
            if (newTrades.Count > 0)
                Trades.AddRange(newTrades);
        }

        private async Task<Trade> UpdateTrade(Trade trade)
        {
            var tradeAction = Strategy.CalculateTradeAction(Ticker, trade);
            switch (tradeAction.TradeAdvice)
            {
                case TradeAdvice.Buy:
                    await _pushManager.TriggerPush(PushMessage.FromMessage($"Got buy signal {tradeAction.Reason}"));
                    if (tradeAction.Reason == TradeReason.BuyTrigger)
                    {
                        Console.WriteLine($"Buy trigger at {Ticker.Bid}");
                        return new Trade { Status = TradeStatus.Empty };
                    }
                    var buyOrder = await CreateBuyOrder(tradeAction.OrderPricePerUnit);
                    trade.BuyOrder = buyOrder;
                    trade.Status = TradeStatus.Buying;
                    break;
                case TradeAdvice.Sell:
                    await _pushManager.TriggerPush(PushMessage.FromMessage($"Got sell signal {tradeAction.Reason}"));
                    await CreateSellOrder(trade, tradeAction.OrderPricePerUnit);
                    return new Trade { Status = TradeStatus.Empty };
                case TradeAdvice.Cancel:
                    await _pushManager.TriggerPush(PushMessage.FromMessage($"Got cancel signal {tradeAction.Reason}"));
                    Console.WriteLine($"{trade.BuyOrder.Uuid}: Canceling order {trade.BuyOrder.Uuid}");
                    var cancelResponse = await _cryptoApi.CancelOrder(trade.BuyOrder.Uuid);
                    if (cancelResponse.IsSuccessful)
                    {
                        Budget.Available += trade.BuyOrder.Price;
                        trade.Status = TradeStatus.Empty;
                    }
                    break;
            }

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
                Uuid = Ticker.Id.ToString()
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
            if (Budget.Available < Strategy.Settings.TradingBudget)
            {
                Budget.Available += Strategy.Settings.TradingBudget;
                Budget.Invested += Strategy.Settings.TradingBudget;
            }
            var priceWithoutCommission = Strategy.Settings.TradingBudget * Consts.BittrexCommission;
            var quantity = priceWithoutCommission / pricePerUnit;
            quantity = quantity.RoundSatoshi();
            var buyOrder = new CryptoOrder
            {
                PricePerUnit = pricePerUnit,
                Price = Strategy.Settings.TradingBudget,
                Quantity = quantity,
                IsOpened = true,
                Market = Market,
                Limit = pricePerUnit,
                Opened = Ticker.Timestamp,
                OrderType = CryptoOrderType.LimitBuy,
                Uuid = Ticker.Id.ToString()
            };
            var buyResponse = await _cryptoApi.BuyCoinAsync(buyOrder);
            if (buyResponse.IsSuccessful)
            {
                Budget.Available -= buyResponse.Content.Price;
            }
            else
            {
                throw new Exception(buyResponse.ErrorMessage);
            }

            return buyResponse.Content;
        }
    }
}
