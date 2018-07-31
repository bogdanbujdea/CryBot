using CryBot.Contracts;

using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Core.Services
{
    public interface ICryptoTrader
    {
        string Market { get; }

        Ticker Ticker { get; }

        List<Trade> Trades { get; }

        TraderSettings Settings { get; }

        Task StartAsync(string market);

        Task UpdatePrice(Ticker ticker);

        Task ProcessMarketUpdates();
    }
}
