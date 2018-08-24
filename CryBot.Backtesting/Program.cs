using CryBot.Core.Trader.Backtesting;

using System;
using System.Threading.Tasks;

namespace CryBot.Backtesting
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var backtester = new BackTester();
            var backtestingStats = await backtester.FindBestSettings("BTC-ETC");
            Console.WriteLine($"{backtestingStats[0].Key}\t{backtestingStats[0].Value.Profit}");
        }
    }
}
