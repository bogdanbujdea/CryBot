using Bittrex.Net.Objects;

using CryBot.Core.Exchange;
using CryBot.Core.Strategies;
using CryBot.Core.Exchange.Models;

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using CryBot.Core.Storage;

namespace CryBot.Core.Trader.Backtesting
{
    public class BackTester
    {
        private readonly FakeBittrexApi _fakeBittrexApi;
        private string _market;
        private List<Candle> _candles;
        private readonly ICryptoApi _cryptoApi;
        private static volatile object _syncObject = new object();

        public BackTester()
        {
            _fakeBittrexApi = new FakeBittrexApi(null);
            _fakeBittrexApi.IsInTestMode = true;
        }

        public async Task<List<KeyValuePair<string, Budget>>> FindBestSettings(string market)
        {
            _market = market;
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
                                  buyTriggerRange.Count * minimumTakeProfitRange.Count * expirationRange.Count;

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
            int oldPercentage = -1;
            Parallel.ForEach(strategies, (strategy) =>
            {
                try
                {
                    var coinTrader = RunHistoryData(strategy).Result;
                    var budget = coinTrader.FinishTest().Result;
                    //Debug.WriteLine($"Finished {index++}");
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
                        Console.WriteLine($"{bestProfit}%\t{bestSettings.ToString()}\tCurrent iteration: {it}/{totalIterations}\t{percentage}%");
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
            return topSettings;
        }
        

        private async Task<CoinTrader> RunHistoryData(ITradingStrategy strategy)
        {
            FakeBittrexApi fakeBittrexApi = new FakeBittrexApi(null)
            {
                IsInTestMode = true,
                Candles = _candles
            };
            var coinTrader = new CoinTrader(fakeBittrexApi)
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
            //Console.WriteLine($"Profit: {coinTrader.TraderState.Budget.Profit}%");
            return coinTrader;
        }
    }
}