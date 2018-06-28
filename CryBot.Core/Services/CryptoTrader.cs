using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CryBot.Core.Models;

namespace CryBot.Core.Services
{
    public class CryptoTrader
    {
        private readonly ICryptoApi _cryptoApi;

        public CryptoTrader(ICryptoApi cryptoApi)
        {
            _cryptoApi = cryptoApi;
        }

        public string Market { get; set; }

        public List<Trade> Trades { get; set; }
        
        public Ticker Ticker { get; set; }

        public async Task StartAsync()
        {
            _cryptoApi.MarketsUpdated += MarketsUpdated;
            if (Trades.Count > 0)
                return;
            var ticker = await _cryptoApi.GetTickerAsync(Market);
            await _cryptoApi.BuyCoinAsync(new CryptoOrder { PricePerUnit = ticker.Content.Last, Price = 0.0012M});
            await _cryptoApi.BuyCoinAsync(new CryptoOrder { PricePerUnit = ticker.Content.Last * 0.98M, Price = 0.0012M});
        }

        private void MarketsUpdated(object sender, List<Ticker> e)
        {
            var currentMarket = e.FirstOrDefault(m => m.Market == Market);
            Ticker = currentMarket;
        }
    }
}
