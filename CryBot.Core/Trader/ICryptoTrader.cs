using CryBot.Core.Strategies;
using CryBot.Core.Exchange.Models;

using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Core.Trader
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
