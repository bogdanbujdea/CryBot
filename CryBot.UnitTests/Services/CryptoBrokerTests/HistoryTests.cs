using Bittrex.Net.Objects;

using CryBot.Core.Trader;
using CryBot.Core.Exchange;
using CryBot.Core.Strategies;

using FluentAssertions;

using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CryBot.Core.Exchange.Models;
using CryBot.Core.Storage;
using Xunit;

namespace CryBot.UnitTests.Services.CryptoBrokerTests
{
    public class HistoryTests
    {
        private readonly FakeBittrexApi _fakeBittrexApi;
        private string _market;
        private List<Candle> _candles;

        [ExcludeFromCodeCoverage]
        public HistoryTests()
        {
            _fakeBittrexApi = new FakeBittrexApi(null);
            _fakeBittrexApi.IsInTestMode = true;
            _market = "BTC-ETC";
        }

        [ExcludeFromCodeCoverage]
        [Fact]
        public async Task CheckProfit()
        {
            _candles = (await _fakeBittrexApi.GetCandlesAsync((_market), TickInterval.OneHour)).Content;
            var strategy = new HoldUntilPriceDropsStrategy
            {
                Settings = new TraderSettings
                {
                    BuyLowerPercentage = 0,
                    TradingBudget = 0.0012M,
                    MinimumTakeProfit = 0M,
                    HighStopLossPercentage = -0.001M,
                    StopLoss = -15,
                    BuyTrigger = -43M,
                    ExpirationTime = TimeSpan.FromHours(2)
                }
            };

            var coinTrader = await RunHistoryData(strategy);
            var budget = await coinTrader.FinishTest();
            budget.Profit.Should().Be(5.86M);
        }

        [ExcludeFromCodeCoverage]
        [Fact]
        public async Task FindBestSettings()
        {
            _candles = (await _fakeBittrexApi.GetCandlesAsync((_market), TickInterval.OneHour)).Content;
            var buyLowerRange = new List<decimal> { -3, -2, -1, 0 };
            var minimumTakeProfitRange = new List<decimal> { 0, 1 };
            var highStopLossRange = new List<decimal> { -5, -1, -0.1M, -0.001M };
            var stopLossRange = new List<decimal> { -15, -6, -4, };
            var buyTriggerRange = new List<decimal> { -4, -2 };
            var expirationRange = new List<TimeSpan>
            {
                TimeSpan.FromMinutes(15),
                TimeSpan.FromHours(1),
                TimeSpan.FromHours(2),
                TimeSpan.FromHours(24)
            };
            var bestSettings = TraderSettings.Default;
            var bestProfit = -1000M;
            var totalIterations = buyLowerRange.Count * highStopLossRange.Count * stopLossRange.Count *
                                  buyTriggerRange.Count * minimumTakeProfitRange.Count;

            var it = 0;
            var strategies = new List<HoldUntilPriceDropsStrategy>();
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
                                    var strategy = new HoldUntilPriceDropsStrategy();
                                    strategy.Settings = new TraderSettings
                                    {
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
            var dict = new Dictionary<string, Budget>();
            var oldPercentage = -1;
            var index = 1;
            Parallel.ForEach(strategies.Take(1), (strategy) =>
            {
                try
                {
                    /*strategy = new HoldUntilPriceDropsStrategy
                    {
                        Settings = new TraderSettings
                        {
                            BuyLowerPercentage = 0,
                            TradingBudget = 0.0012M,
                            MinimumTakeProfit = 0M,
                            HighStopLossPercentage = -0.001M,
                            StopLoss = -15,
                            BuyTrigger = -43M,
                            ExpirationTime = TimeSpan.FromHours(2)
                        }
                    };*/
                    var coinTrader = RunHistoryData(strategy).Result;
                    var budget = coinTrader.FinishTest().Result;
                    Debug.WriteLine($"Finished {index++}");
                    it++;
                    if (budget.Profit > bestProfit)
                    {
                        bestSettings = strategy.Settings;
                        bestProfit = budget.Profit;
                    }

                    dict[strategy.Settings.ToString()] = budget;
                    var percentage = (it * 100) / totalIterations;
                    if (percentage != oldPercentage)
                    {
                        oldPercentage = percentage;
                        Console.WriteLine($"{bestProfit}%\t\t{percentage}%\t\t{bestSettings.ToString()}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });

            var topSettings = dict.OrderByDescending(d => d.Value.Profit).Take(50).ToList();
            foreach (var keyValuePair in topSettings)
            {
                Console.WriteLine($"{keyValuePair.Value.Profit}% - {keyValuePair.Key}\t{keyValuePair.Value.Invested}\t{keyValuePair.Value.Earned}");
            }
            Console.WriteLine($"Best settings {bestSettings.StopLoss}\t{bestProfit} BTC");

            var trader = await RunHistoryData(new HoldUntilPriceDropsStrategy { Settings = bestSettings });
            var traderBudget = await trader.FinishTest();
            traderBudget.Profit.Should().Be(topSettings[0].Value.Profit);
        }

        [ExcludeFromCodeCoverage]
        private async Task<CryptoBroker> RunHistoryData(ITradingStrategy strategy)
        {
            FakeBittrexApi fakeBittrexApi = new FakeBittrexApi(null);
            fakeBittrexApi.IsInTestMode = true;
            fakeBittrexApi.Candles = _candles;
            var coinTrader = new CryptoBroker(fakeBittrexApi)
            {
                IsInTestMode = true
            };
            _fakeBittrexApi.IsInTestMode = true;
            coinTrader.Initialize(new TraderState
            {
                Market = _market
            });
            coinTrader.Strategy = strategy;

            await fakeBittrexApi.SendMarketUpdates(_market);
            Console.WriteLine($"Profit: {coinTrader.TraderState.Budget.Profit}%");
            return coinTrader;
        }
    }
}
