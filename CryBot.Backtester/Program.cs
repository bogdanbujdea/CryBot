using Bittrex.Net.Objects;

using CryBot.Core.Models;
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
            backtester.Strategy.Settings.BuyLowerPercentage = -1;
            backtester.Strategy.Settings.MinimumTakeProfit = 0M;
            backtester.Strategy.Settings.HighStopLossPercentage = -0.1M;
            backtester.Strategy.Settings.StopLoss = -4;
            backtester.Strategy.Settings.BuyTrigger= -2;
            backtester.Candles = cryptoResponse.Content;
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
