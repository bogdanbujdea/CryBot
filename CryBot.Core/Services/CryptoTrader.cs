using System;
using CryBot.Core.Models;
using CryBot.Core.Utilities;

using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Core.Services
{
    public class CryptoTrader
    {
        private readonly ICryptoApi _cryptoApi;

        public CryptoTrader(ICryptoApi cryptoApi)
        {
            _cryptoApi = cryptoApi;
            Trades = new List<Trade>();
        }

        public string Market { get; set; }

        public List<Trade> Trades { get; set; }

        public Ticker Ticker { get; set; }

        public TraderSettings Settings { get; set; }

        public async Task StartAsync()
        {
            _cryptoApi.MarketsUpdated += MarketsUpdated;
            _cryptoApi.OrderUpdated += OrderUpdated;
            if (Trades.Count > 0)
                return;
            var tickerResponse = await _cryptoApi.GetTickerAsync(Market);
            Ticker = tickerResponse.Content;
            await CreateBuyOrder(tickerResponse.Content.Ask);
            await CreateBuyOrder(tickerResponse.Content.Bid * Settings.BuyLowerPercentage.ToPercentageMultiplier());
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
                    Console.WriteLine($"Closed sell order");
                    tradeForOrder.IsActive = false;
                    await CreateBuyOrder(e.PricePerUnit * Settings.BuyLowerPercentage.ToPercentageMultiplier());
                }
            }
            else
            {
                var tradeForOrder = Trades.FirstOrDefault(t => t.BuyOrder.Uuid == e.Uuid);
                if (tradeForOrder != null)
                {
                    Console.WriteLine($"Closed buy order");
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
            Ticker = currentMarket;
            await UpdateTrades();
        }

        private async Task UpdateTrades()
        {
            foreach (var trade in Trades.Where(t => t.IsActive))
            {
                Ticker.Bid = Ticker.Last;
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

        private async Task CreateSellOrder(Trade trade)
        {
            Console.WriteLine($"Creating sell order");
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
