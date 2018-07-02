using CryBot.Core.Models;
using CryBot.Core.Services;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace CryBot.Web.Infrastructure
{
        public class CryptoHostedService : IHostedService, IDisposable
    {
        private readonly IOptions<EnvironmentConfig> _options;
        private readonly ICryptoApi _cryptoApi;

        private CancellationTokenSource _cancellationTokenSource;
        private Guid _hostedServiceId;

        public CryptoHostedService(IOptions<EnvironmentConfig> options, ICryptoApi cryptoApi)
        {
            _options = options;
            _cryptoApi = cryptoApi;
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
            var cryptoTrader = new CryptoTrader(_cryptoApi)
            {
                Market = "BTC-ETC",
                Settings = new TraderSettings
                {
                    BuyLowerPercentage = -2,
                    DefaultBudget = 0.0012M,
                    MinimumTakeProfit = 0.1M,
                    HighStopLossPercentage = -5,
                    StopLoss = -2
                }
            };
            await cryptoTrader.StartAsync();
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
