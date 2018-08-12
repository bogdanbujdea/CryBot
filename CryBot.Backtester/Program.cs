using Bittrex.Net.Objects;

using CryBot.Core.Models;
using CryBot.Core.Services;
using CryBot.Backtester.Properties;

using System;
using System.Linq;
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
                bittrexApi.Initialize(Resources.BittrexApiKey, Resources.BittrexApiSecret, true);
                await TestStrategy(bittrexApi, market);
                return;
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

        private static async Task TestStrategy(BittrexApi bittrexApi, string market)
        {
            var cryptoResponse = await bittrexApi.GetCandlesAsync(market, TickInterval.OneHour);
            var backtester = new CryptoTraderBacktester();
            backtester.Strategy = new HoldUntilPriceDropsStrategy
            {
                Settings = TraderSettings.Default
            };
            backtester.Strategy.Settings.BuyLowerPercentage = 0;
            backtester.Strategy.Settings.TradingBudget = 0.0012M;
            backtester.Strategy.Settings.MinimumTakeProfit = 0M;
            backtester.Strategy.Settings.HighStopLossPercentage = -0.001M;
            backtester.Strategy.Settings.StopLoss = -15;
            backtester.Strategy.Settings.BuyTrigger = -43M;
            backtester.Strategy.Settings.ExpirationTime = TimeSpan.FromHours(2);
            var response = await new FakeBittrexApi(null).GetCandlesAsync(market, TickInterval.OneMinute);
            backtester.Candles = response.Content.Take(5000).ToList();
            /*backtester.Candles = new List<Candle>
            {
                new Candle{ Low = 0.00010000M, High = 0.00010100M },
                new Candle{ Low = 0.00009900M, High = 0.00010000M },
                new Candle{ Low = 0.00010100M, High = 0.00010200M },
                new Candle{ Low = 0.00010800M, High = 0.00011000M },
                new Candle{ Low = 0.00010500M, High = 0.00010600M },
                new Candle{ Low = 0.00010394M, High = 0.00010395M },
                new Candle{ Low = 0.00009975M, High = 0.00009976M },
                new Candle{ Low = 0.00009975M, High = 0.00009976M },
                
                
                
                /*new Candle{ Low = 0.00008079M, High = 0.00008079M },
                new Candle{ Low = 0.00008079M, High = 0.00008079M },
                new Candle{ Low = 0.00008522M, High = 0.00008522M },
                new Candle{ Low = 0.00008124M, High = 0.00008124M },
                new Candle{ Low = 0.00008124M, High = 0.00008124M },#1#
            };*/
            backtester.Initialize();
            var cryptoTraderStats = backtester.StartFromFile(market);
            Console.WriteLine($"Profit: {cryptoTraderStats.Profit}");
            Console.ReadLine();
        }
    }
}
