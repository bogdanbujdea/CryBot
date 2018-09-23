using CryBot.Core.Strategies;
using CryBot.Core.Exchange.Models;

using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Core.Trader.Backtesting
{
    public interface IBackTester
    {
        Task<List<BackTestResult>> FindBestSettings(string market, List<Candle> candlesContent = null);
        Task<BackTestResult> TrySettings(string market, List<Candle> candlesContent, TraderSettings settings);
    }
}