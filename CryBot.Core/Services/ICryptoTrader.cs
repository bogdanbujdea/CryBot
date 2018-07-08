using CryBot.Contracts;

using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Core.Services
{
    public interface ICryptoTrader
    {
        string Market { get; set; }
        List<Trade> Trades { get; set; }
        Ticker Ticker { get; set; }
        TraderSettings Settings { get; set; }
        Task StartAsync();
    }
}
