using CryBot.Core.Storage;
using CryBot.Core.Exchange;
using CryBot.Core.Notifications;
using CryBot.Core.Exchange.Models;

using Orleans;

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Bittrex.Net.Objects;

namespace CryBot.Core.Trader
{
    public class TradersManager : ITradersManager
    {
        private readonly ICryptoApi _cryptoApi;
        private readonly ITradersRepository _tradersRepository;
        private readonly IClusterClient _clusterClient;
        private readonly IHubNotifier _hubNotifier;
        private readonly IPushManager _pushManager;

        public TradersManager(ICryptoApi cryptoApi, ITradersRepository tradersRepository, IClusterClient clusterClient, IHubNotifier hubNotifier, IPushManager pushManager)
        {
            _cryptoApi = cryptoApi;
            _tradersRepository = tradersRepository;
            _clusterClient = clusterClient;
            _hubNotifier = hubNotifier;
            _pushManager = pushManager;
        }

        public async Task<List<TraderState>> GetAllTraders()
        {
            try
            {
                var marketsResponse = await _tradersRepository.GetTradedMarketsAsync();
                var traderStates = new List<TraderState>();
                foreach (var market in marketsResponse.Content.Select(m => m.Name))
                {
                    var traderGrain = _clusterClient.GetGrain<ITraderGrain>(market);
                    await traderGrain.SetMarketAsync(market);
                    traderStates.Add(await traderGrain.GetTraderData());
                }

                return traderStates;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return new List<TraderState>();
            }
        }

        public async Task CreateTraderAsync(string market)
        {
            var coinTrader = new LiveTrader(_clusterClient, _hubNotifier, _pushManager, new CoinTrader(_cryptoApi), _cryptoApi);
            coinTrader.Initialize(market);
            await _tradersRepository.CreateTraderAsync(new Market { Name = market });
            await coinTrader.StartAsync();
        }

        public async Task<Chart> GetChartAsync(string market)
        {
            var traderGrain = _clusterClient.GetGrain<ITraderGrain>(market);
            var traderState = await traderGrain.GetTraderData();
            var chart = new Chart();
            var candlesResponse = await _cryptoApi.GetCandlesAsync(market, TickInterval.OneHour);
            chart.Candles = candlesResponse.Content;
            chart.Trades = traderState.Trades;
            return chart;
        }
    }
}