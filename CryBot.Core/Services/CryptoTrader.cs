using CryBot.Contracts;
using CryBot.Core.Models;
using CryBot.Core.Utilities;
using Orleans;

using System;

using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Core.Services
{
    public class CryptoTrader
    {
        private readonly ICryptoApi _cryptoApi;
        private readonly IClusterClient _orleansClient;
        private readonly IHubNotifier _hubNotifier;
        private ITraderGrain _traderGrain;

        public CryptoTrader(ICryptoApi cryptoApi, IClusterClient orleansClient, IHubNotifier hubNotifier)
        {
            _cryptoApi = cryptoApi;
            _orleansClient = orleansClient;
            _hubNotifier = hubNotifier;
            _hubNotifier = hubNotifier;
            Trades = new List<ITrade>();
        }

        public string Market { get; private set; }

        public Ticker Ticker { get; private set; }

        public List<ITrade> Trades { get; private set; }

        public ITraderSettings Settings { get; private set; }

        public async Task StartAsync(string market)
        {
            _traderGrain = _orleansClient.GetGrain<ITraderGrain>(market);
            Market = market;
            _cryptoApi.MarketsUpdated += MarketsUpdated;
            _cryptoApi.OrderUpdated += OrderUpdated;
            Trades = await _traderGrain.GetActiveTrades();
            var tickerResponse = await _cryptoApi.GetTickerAsync(Market);
            await _traderGrain.UpdatePriceAsync(tickerResponse.Content);
            Ticker = tickerResponse.Content;
            Settings = await _traderGrain.GetSettings();
            if (Trades.Count > 0)
                return;
            await CreateBuyOrder(tickerResponse.Content.Ask);
            await CreateBuyOrder(tickerResponse.Content.Bid * Settings.BuyLowerPercentage.ToPercentageMultiplier());
        }

        public async Task UpdatePrice(Ticker ticker)
        {
            Ticker = ticker;
            await _traderGrain.UpdatePriceAsync(ticker);
        }

        private async Task CreateBuyOrder(decimal pricePerUnit)
        {
            var trade = new Trade();
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
            Trades.Add(trade);
        }

        private async void OrderUpdated(object sender, CryptoOrder e)
        {
            if (Trades.Count == 0)
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
        }

        private async void MarketsUpdated(object sender, List<Ticker> e)
        {
            var currentMarket = e.FirstOrDefault(m => m.Market == Market);
            if (currentMarket == null)
                return;
            await _hubNotifier.SendTicker(currentMarket);
            await _traderGrain.UpdatePriceAsync(currentMarket);
            Ticker = currentMarket;
            await UpdateTrades();
        }

        private async Task UpdateTrades()
        {
            foreach (var trade in Trades.Where(t => t.IsActive))
            {
                if (trade.MaxPricePerUnit < Ticker.Bid)
                {
                    Console.WriteLine($"New max for {Market}: {Ticker.Bid}");
                    trade.MaxPricePerUnit = Ticker.Bid;
                }

                Console.WriteLine($"{Market}: {trade.BuyOrder.PricePerUnit.GetReadablePercentageChange(Ticker.Bid, true)}\t{trade.BuyOrder.PricePerUnit}\t{Ticker.Bid}");
                if (Ticker.Bid.ReachedHighStopLoss(trade.MaxPricePerUnit,
                    trade.BuyOrder.PricePerUnit * Settings.MinimumTakeProfit.ToPercentageMultiplier(),
                    Settings.HighStopLossPercentage.ToPercentageMultiplier(), trade.BuyOrder.PricePerUnit))
                {
                    await CreateSellOrder(trade);
                }

                if (Ticker.Bid.ReachedStopLoss(trade.BuyOrder.PricePerUnit, Settings.StopLoss))
                {
                    await CreateSellOrder(trade);
                }
            }
        }

        private async Task CreateSellOrder(ITrade trade)
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
