using CryBot.Core.Services;
using CryBot.Backtester.Properties;

using System;
using System.Threading.Tasks;

namespace CryBot.Backtester
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                Console.Write("Market: ");
                var market = Console.ReadLine();
                var bittrexApi = new BittrexApi(null);                
                bittrexApi.Initialize(Resources.BittrexApiKey, Resources.BittrexApiSecret);
                var backTester = new BackTester(bittrexApi);
                var stats = await backTester.FindBestSettings(market);
                Console.WriteLine($"Best settings for market {market} are {stats.TradingStrategy.Settings} with profit of {stats.TraderStats.Profit}");
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
