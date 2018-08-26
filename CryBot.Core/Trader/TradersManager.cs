using CryBot.Core.Storage;
using CryBot.Core.Exchange;
using CryBot.Core.Notifications;

using Orleans;

using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Core.Trader
{
    public class TradersManager : ITradersManager
    {
        private readonly ICryptoApi _cryptoApi;
        private readonly IClusterClient _clusterClient;
        private readonly IHubNotifier _hubNotifier;
        private readonly IPushManager _pushManager;

        public TradersManager(ICryptoApi cryptoApi, IClusterClient clusterClient, IHubNotifier hubNotifier, IPushManager pushManager)
        {
            _cryptoApi = cryptoApi;
            _clusterClient = clusterClient;
            _hubNotifier = hubNotifier;
            _pushManager = pushManager;
        }

        public async Task<List<TraderState>> GetAllTraders()
        {
            var marketsResponse = await _cryptoApi.GetMarketsAsync();
            var traderStates = new List<TraderState>();
            foreach (var market in marketsResponse.Content.Select(m => m.Name))
            {
                var traderGrain = _clusterClient.GetGrain<ITraderGrain>(market);
                await traderGrain.SetMarketAsync(market);
                if (await traderGrain.IsInitialized())
                    traderStates.Add(await traderGrain.GetTraderData());
            }

            return traderStates;
        }

        public async Task CreateTraderAsync(string market)
        {
            var coinTrader = new LiveTrader(_clusterClient, _hubNotifier, _pushManager, new CoinTrader(_cryptoApi), _cryptoApi);
            coinTrader.Initialize(market);
            await coinTrader.StartAsync();
        }
    }
}