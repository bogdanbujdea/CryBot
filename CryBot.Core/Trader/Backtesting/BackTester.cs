using Bittrex.Net.Objects;

using CryBot.Core.Storage;
using CryBot.Core.Exchange;
using CryBot.Core.Strategies;
using CryBot.Core.Infrastructure;
using CryBot.Core.Exchange.Models;

using Microsoft.Extensions.Options;

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Core.Trader.Backtesting
{
    public class BackTester : IBackTester
    {
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private readonly FakeBittrexApi _fakeBittrexApi;
        private string _market;
        private List<Candle> _candles;
        private List<Candle> _hourlyCandles;

        public BackTester(IOptions<EnvironmentConfig> config): this(config.Value.BittrexApiKey, config.Value.BittrexApiSecret)
        {
        }

        public BackTester(string apiKey, string apiSecret)
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;
            _fakeBittrexApi = new FakeBittrexApi(apiKey, apiSecret);
            _fakeBittrexApi.IsInTestMode = true;
        }

        public async Task<List<BackTestResult>> FindBestSettings(string market, List<Candle> candlesContent = null)
        {
            _market = market;
            var candlesResponse = await _fakeBittrexApi.GetCandlesAsync(_market, TickInterval.OneHour);
            _hourlyCandles = candlesResponse.Content;
            if (candlesContent == null)
                _candles = (await _fakeBittrexApi.GetCandlesAsync((_market), TickInterval.OneMinute)).Content;
            else _candles = candlesContent;
            
            var firstOrderBuyLowerRange = new List<decimal> { -1, 0 };
            var buyLowerRange = new List<decimal> { -3, -2, -1, -0.5M };
            var minimumTakeProfitRange = new List<decimal> { 0, 0.5M, 1 };
            var highStopLossRange = new List<decimal> { -10, -5, -1 };
            var stopLossRange = new List<decimal> { -25, -6, -4, -2 };
            var buyTriggerRange = new List<decimal> { -4, -2, -1 };
            var expirationRange = new List<TimeSpan>
            {
                TimeSpan.FromHours(1),
                TimeSpan.FromHours(24)
            };
            var it = 0;
            var total = 0;
            var bestProfit = -1000M;
            var strategies = new List<HoldUntilPriceDropsStrategy>();
            foreach (var firstBuy in firstOrderBuyLowerRange)
            {
                foreach (var buy in buyLowerRange)
                {
                    foreach (var highStopLoss in highStopLossRange)
                    {
                        foreach (var stopLoss in stopLossRange)
                        {
                            foreach (var trigger in buyTriggerRange)
                            {
                                foreach (var expiration in expirationRange)
                                {
                                    foreach (var minProfit in minimumTakeProfitRange)
                                    {
                                        if (stopLoss <= trigger)
                                            continue;
                                        total++;
                                        var strategy = new HoldUntilPriceDropsStrategy();
                                        strategy.Settings = new TraderSettings
                                        {
                                            FirstBuyLowerPercentage = firstBuy,
                                            BuyLowerPercentage = buy,
                                            HighStopLossPercentage = highStopLoss,
                                            StopLoss = stopLoss,
                                            BuyTrigger = trigger,
                                            MinimumTakeProfit = minProfit,
                                            TradingBudget = TraderSettings.Default.TradingBudget,
                                            ExpirationTime = expiration
                                        };
                                        strategies.Add(strategy);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            int oldPercentage = -1;
            var results = new List<BackTestResult>();
            Parallel.ForEach(strategies.Take(1), (strategy) =>
            {
                try
                {
                    it++;
                    strategy.Settings = TraderSettings.Default;
                    var coinTrader = RunHistoryData(strategy).Result;
                    var budget = coinTrader.FinishTest().Result;
                    var backTestResult = new BackTestResult
                    {
                        Budget = budget,
                        Settings = strategy.Settings
                    };
                    results.Add(backTestResult);
                    var percentage = (it * 100) / total;
                    if (percentage != oldPercentage)
                    {
                        oldPercentage = percentage;
                        if (bestProfit < budget.Profit)
                        {
                            bestProfit = budget.Profit;
                        }
                        Console.WriteLine($"Current iteration: {it}/{total}\t{percentage}%\t\t{bestProfit}%");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });
            results = results.OrderByDescending(r => r.Budget.Profit).ToList();
            var uniqueProfits = results.GroupBy(r => r.Budget.Profit).OrderByDescending(r => r.Key).Select(s => s.FirstOrDefault()).ToList();
            Console.WriteLine($"{uniqueProfits.Count}/{results.Count}");
            foreach (var result in uniqueProfits.Take(50))
            {
                Console.WriteLine($"{result.Budget.Profit}% - {result.Settings}\t{result.Budget.Invested}\t{result.Budget.Earned}");
            }
            return uniqueProfits;
        }

        public async Task<BackTestResult> TrySettings(string market, List<Candle> candlesContent, TraderSettings settings)
        {
            _market = market;
            if (candlesContent == null)
                _candles = (await _fakeBittrexApi.GetCandlesAsync((_market), TickInterval.OneMinute)).Content;
            else _candles = candlesContent;
            var strategy = new HoldUntilPriceDropsStrategy{Settings = settings};
            var coinTrader = await RunHistoryData(strategy);
            var budget = await coinTrader.FinishTest();
            var backTestResult = new BackTestResult
            {
                Budget = budget,
                Settings = strategy.Settings
            };
            return backTestResult;
        }


        private async Task<CoinTrader> RunHistoryData(ITradingStrategy strategy)
        {
            FakeBittrexApi fakeBittrexApi = new FakeBittrexApi(_apiKey, _apiSecret)
            {
                IsInTestMode = true,
                Candles = _candles
            };
            var coinTrader = new CoinTrader(fakeBittrexApi)
            {
                IsInTestMode = true
            };
            _fakeBittrexApi.IsInTestMode = true;
            coinTrader.Candles = _hourlyCandles;
            coinTrader.Initialize(new TraderState
            {
                Market = _market
            });
            coinTrader.Strategy = strategy;

            await fakeBittrexApi.SendMarketUpdates(_market);
            return coinTrader;
        }
    }
}