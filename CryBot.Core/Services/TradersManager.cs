using CryBot.Contracts;
using Orleans;

using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Core.Services
{
    public class TradersManager : ITradersManager
    {
        private readonly ICryptoApi _cryptoApi;
        private readonly IClusterClient _clusterClient;
        private readonly IHubNotifier _hubNotifier;

        public TradersManager(ICryptoApi cryptoApi, IClusterClient clusterClient, IHubNotifier hubNotifier)
        {
            _cryptoApi = cryptoApi;
            _clusterClient = clusterClient;
            _hubNotifier = hubNotifier;
        }

        public async Task<List<TraderState>> GetAllTraders()
        {
            var marketsResponse = await _cryptoApi.GetMarketsAsync();
            var traderStates = new List<TraderState>();
            foreach (var market in marketsResponse.Content.Select(m => m.Name))
            {
                var traderGrain = _clusterClient.GetGrain<ITraderGrain>(market);
                if (await traderGrain.IsInitialized())
                    traderStates.Add(await traderGrain.GetTraderData());
            }

            return traderStates;
        }

        public async Task CreateTraderAsync(string market)
        {
            var cryptoTrader = new CryptoTrader(_cryptoApi, _clusterClient, _hubNotifier);
            await cryptoTrader.StartAsync(market);
        }
    }
}