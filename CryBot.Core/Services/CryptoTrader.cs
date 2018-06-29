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
            if (Trades.Count > 0)
                return;
            var tickerResponse = await _cryptoApi.GetTickerAsync(Market);
            Ticker = tickerResponse.Content;
            await _cryptoApi.BuyCoinAsync(new CryptoOrder { PricePerUnit = tickerResponse.Content.Last, Price = 0.0012M});
            await _cryptoApi.BuyCoinAsync(new CryptoOrder { PricePerUnit = tickerResponse.Content.Last * 0.98M, Price = 0.0012M});
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
                    await _cryptoApi.SellCoinAsync(CreateSellOrder(trade));
                }

                if (Ticker.Last.ReachedStopLoss(trade.BuyOrder.PricePerUnit, Settings.StopLoss))
                {
                    await _cryptoApi.SellCoinAsync(CreateSellOrder(trade));
                }
            }
        }

        private CryptoOrder CreateSellOrder(Trade lastTrade)
        {
            return new CryptoOrder
            {
                PricePerUnit = Ticker.Last,
                Price = Ticker.Last * lastTrade.BuyOrder.Quantity,
                Market = Market,
                Quantity = lastTrade.BuyOrder.Quantity
            };
        }
    }
}
