using System.Collections.Generic;
using System.Threading.Tasks;
using CryBot.Core.Exchange.Models;
using CryBot.Core.Strategies;

namespace CryBot.Core.Trader.Backtesting
{
    public interface IBackTester
    {
        Task<List<BackTestResult>> FindBestSettings(string market, List<Candle> candlesContent = null);
        Task<BackTestResult> TrySettings(string market, List<Candle> candlesContent, TraderSettings settings);
    }
}