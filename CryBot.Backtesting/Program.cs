using CryBot.Core.Trader.Backtesting;

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Backtesting
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var backtester = new BackTester(Resource.BittrexApiKey, Resource.BittrexApiSecret);
            var market = "BTC-ETC";
            var marketResults = await backtester.FindBestSettings(market);
            var bestResult = marketResults[0];
            Console.WriteLine($"{market}\t{bestResult.Budget.Profit}%\t{bestResult.Settings}");
        }

        private static async Task FindBestCommonSettings(BackTester backtester)
        {
            var otherMarkets = new List<string>
            {
                "BTC-ETC",
                "BTC-ETH",
                "BTC-ADA",
                "BTC-XLM",
                "BTC-XRP",
                "BTC-XMR",
                "BTC-LTC",
                "BTC-TRX"
            };
            var topSettings = new List<BackTestResult>();
            foreach (var btcMarket in otherMarkets)
            {
                var marketResults = await backtester.FindBestSettings(btcMarket);
                var bestResult = marketResults[0];
                topSettings.Add(bestResult);
                Console.WriteLine($"{btcMarket}\t{bestResult.Budget.Profit}%\t{bestResult.Settings}");
                foreach (var testMarket in otherMarkets.Where(m => m != btcMarket))
                {
                    bestResult = await GetResultForMarket(testMarket, marketResults[0]);
                    Console.WriteLine($"{btcMarket}---{testMarket}\t{bestResult.Budget.Profit}%");
                }
            }
        }

        private static async Task<BackTestResult> GetResultForMarket(string btcMarket, BackTestResult bestEtcBacktest)
        {
            var backtester = new BackTester(Resource.BittrexApiKey, Resource.BittrexApiSecret);
            var bestResult = await backtester.TrySettings(btcMarket, null, bestEtcBacktest.Settings);
            return bestResult;
        }
    }
}
