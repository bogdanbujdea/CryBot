using CryBot.Core.Exchange;
using CryBot.Core.Exchange.Models;
using CryBot.Core.Notifications;
using CryBot.Core.Storage;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CryBot.Core.Trader.Backtesting;

namespace CryBot.Core.Trader
{
    public class TradersManager : ITradersManager
    {
        private readonly ICryptoApi _cryptoApi;
        private readonly ITradersRepository _tradersRepository;
        private readonly IBackTester _backTester;
        private readonly IClusterClient _clusterClient;
        private readonly IHubNotifier _hubNotifier;
        private readonly IPushManager _pushManager;

        public TradersManager(ICryptoApi cryptoApi, ITradersRepository tradersRepository, IBackTester backTester, IClusterClient clusterClient, IHubNotifier hubNotifier, IPushManager pushManager)
        {
            _cryptoApi = cryptoApi;
            _tradersRepository = tradersRepository;
            _backTester = backTester;
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
                    if (await traderGrain.IsInitialized())
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
            var coinTrader = new LiveTrader(_clusterClient, _hubNotifier, _pushManager, new CoinTrader(_cryptoApi), _backTester);
            coinTrader.Initialize(market);
            await _tradersRepository.CreateTraderAsync(new Market { Name = market });
            await coinTrader.StartAsync();
        }
    }
}