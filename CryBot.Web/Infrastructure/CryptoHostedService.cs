using CryBot.Core.Hubs;
using CryBot.Core.Models;
using CryBot.Core.Services;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.SignalR;

using Orleans;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace CryBot.Web.Infrastructure
{
        public class CryptoHostedService : IHostedService, IDisposable
    {
        private readonly IOptions<EnvironmentConfig> _options;
        private readonly ICryptoApi _cryptoApi;
        private readonly IClusterClient _clusterClient;
        private readonly IHubContext<ApplicationHub> _hubContext;

        private CancellationTokenSource _cancellationTokenSource;
        private Guid _hostedServiceId;
        private HubNotifier _hubNotifier;

        public CryptoHostedService(IOptions<EnvironmentConfig> options, ICryptoApi cryptoApi, IClusterClient clusterClient, IHubContext<ApplicationHub> hubContext)
        {
            _options = options;
            _cryptoApi = cryptoApi;
            _clusterClient = clusterClient;
            _hubContext = hubContext;
            _hubNotifier = new HubNotifier(_hubContext);
            _cryptoApi.Initialize(options.Value.BittrexApiKey, options.Value.BittrexApiSecret);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _hostedServiceId = Guid.NewGuid();

            _cancellationTokenSource = new CancellationTokenSource();
            await StartTrading();
        }

        public async Task StartTrading()
        {
            var cryptoTrader = new CryptoTrader(_cryptoApi, _clusterClient, _hubNotifier)
            {
                
            };
            await cryptoTrader.StartAsync("BTC-ETC");
            while (true)
            {
                await Task.Delay(5000);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource.Cancel();
        }

        public void Dispose()
        {
            _cancellationTokenSource.Dispose();
        }
    }
}
