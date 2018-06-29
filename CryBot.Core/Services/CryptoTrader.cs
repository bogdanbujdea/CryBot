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
            await CreateBuyOrder(tickerResponse.Content.Last);
            await CreateBuyOrder(tickerResponse.Content.Last * 0.98M);
        }

        private async Task CreateBuyOrder(decimal pricePerUnit)
        {
            var trade = new Trade();
            var buyResponse = await _cryptoApi.BuyCoinAsync(new CryptoOrder {PricePerUnit = pricePerUnit, Price = 0.0012M});
            trade.BuyOrder = buyResponse.Content;
            trade.IsActive = true;
            Trades.Add(trade);
        }

        private void OrderUpdated(object sender, CryptoOrder e)
        {
            var tradeForOrder = Trades.FirstOrDefault(t => t.SellOrder.Uuid == e.Uuid);
            if (tradeForOrder != null)
            {
                tradeForOrder.IsActive = false;
            }
        }

        private async void MarketsUpdated(object sender, List<Ticker> e)
        {
            var currentMarket = e.FirstOrDefault(m => m.Market == Market);
            Ticker = currentMarket;
            await UpdateTrades();
        }

        private async Task UpdateTrades()
        {
            foreach (var trade in Trades.Where(t => t.IsActive))
            {
                if (trade.MaxPricePerUnit < Ticker.Last)
                    trade.MaxPricePerUnit = Ticker.Last;
                if (Ticker.Last.ReachedHighStopLoss(trade.MaxPricePerUnit,
                    trade.BuyOrder.PricePerUnit * Settings.MinimumTakeProfit.ToPercentageMultiplier(),
                    Settings.HighStopLossPercentage.ToPercentageMultiplier(), trade.BuyOrder.PricePerUnit))
                {
                    await CreateSellOrder(trade);
                }

                if (Ticker.Last.ReachedStopLoss(trade.BuyOrder.PricePerUnit, Settings.StopLoss))
                {
                    await CreateSellOrder(trade);
                }
            }
        }

        private async Task CreateSellOrder(Trade trade)
        {
            var cryptoOrder = new CryptoOrder
            {
                PricePerUnit = Ticker.Last,
                Price = Ticker.Last * trade.BuyOrder.Quantity,
                Market = Market,
                Quantity = trade.BuyOrder.Quantity
            };
            var sellOrderResponse =  await _cryptoApi.SellCoinAsync(cryptoOrder);
            trade.SellOrder = sellOrderResponse.Content;
        }
    }
}
