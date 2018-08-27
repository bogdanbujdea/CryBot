using Bittrex.Net.Objects;

using CryBot.Core.Trader;
using CryBot.Core.Exchange;
using CryBot.Core.Notifications;
using CryBot.Core.Infrastructure;
using CryBot.Core.Trader.Backtesting;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.SignalR;

using Orleans;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CryBot.Web.Infrastructure
{
    public class CryptoHostedService : IHostedService, IDisposable
    {
        private readonly IOptions<EnvironmentConfig> _options;
        private readonly IPushManager _pushManager;
        private readonly IBackTester _backTester;
        private readonly ICryptoApi _cryptoApi;
        private readonly IClusterClient _clusterClient;
        private readonly ITradersManager _tradersManager;

        private CancellationTokenSource _cancellationTokenSource;
        private readonly HubNotifier _hubNotifier;

        public CryptoHostedService(IOptions<EnvironmentConfig> options, IPushManager pushManager, IBackTester backTester, ICryptoApi cryptoApi, IClusterClient clusterClient, IHubContext<ApplicationHub> hubContext, ITradersManager tradersManager)
        {
            _options = options;
            _pushManager = pushManager;
            _backTester = backTester;
            _cryptoApi = cryptoApi;
            _clusterClient = clusterClient;
            _tradersManager = tradersManager;
            _hubNotifier = new HubNotifier(hubContext);
            Console.WriteLine($"Bittrex api key {options.Value.BittrexApiKey}");
            _cryptoApi.Initialize(options.Value.BittrexApiKey, options.Value.BittrexApiSecret, options.Value.TestMode, true);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            _cryptoApi.IsInTestMode = _options.Value.TestMode;
            if (_options.Value.TestMode)
            {
                var market = "BTC-ETC";
                try
                {
                    await _cryptoApi.GetCandlesAsync(market, TickInterval.OneMinute);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                var coinTrader = new LiveTrader(_clusterClient, _hubNotifier, _pushManager, new CoinTrader(_cryptoApi), _backTester);
                coinTrader.Initialize(market);
                coinTrader.IsInTestMode = true;
                await coinTrader.StartAsync();
                await Task.Run(() => _cryptoApi.SendMarketUpdates(market), cancellationToken);
                return;
            }

            await StartTrading();
        }

        public async Task StartTrading()
        {
            await _pushManager.TriggerPush(PushMessage.FromMessage("Started trading"));
            var traderStates = await _tradersManager.GetAllTraders();
            foreach (var market in traderStates.Select(t => t.Market))
            {
                var coinTrader = new LiveTrader(_clusterClient, _hubNotifier, _pushManager, new CoinTrader(_cryptoApi), _backTester);
                coinTrader.Initialize(market);
                await coinTrader.StartAsync();
            }

            Console.WriteLine("Finished loading");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource?.Cancel();
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
        }
    }
}
