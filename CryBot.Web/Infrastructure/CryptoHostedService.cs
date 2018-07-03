using CryBot.Core.Models;
using CryBot.Core.Services;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

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

        private CancellationTokenSource _cancellationTokenSource;
        private Guid _hostedServiceId;

        public CryptoHostedService(IOptions<EnvironmentConfig> options, ICryptoApi cryptoApi, IClusterClient clusterClient)
        {
            _options = options;
            _cryptoApi = cryptoApi;
            _clusterClient = clusterClient;
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
            var cryptoTrader = new CryptoTrader(_cryptoApi, _clusterClient)
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
