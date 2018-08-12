using Bittrex.Net.Objects;

using CryBot.Core.Trader;
using CryBot.Core.Exchange;
using CryBot.Core.Strategies;

using FluentAssertions;

using System;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

namespace CryBot.UnitTests.Services.CoinTraderTests
{
    public class HistoryTests: CoinTraderTestBase
    {
        private CoinTrader CoinTrader;

        [Fact]
        public async Task CheckProfit()
        {
            CoinTrader = await RunHistoryData();
            var budget = await CoinTrader.FinishTest();
            budget.Profit.Should().Be(5.86M);
        }

        private async Task<CoinTrader> RunHistoryData()
        {
            var fakeBittrexApi = new FakeBittrexApi(null);
            await fakeBittrexApi.GetCandlesAsync(Market, TickInterval.OneHour);
            CoinTrader = new CoinTrader(fakeBittrexApi, OrleansClientMock.Object, HubNotifierMock.Object,
                PushManagerMock.Object) {IsInTestMode = true};
            fakeBittrexApi.IsInTestMode = true;
            CoinTrader.Initialize(Market);
            CoinTrader.Strategy = new HoldUntilPriceDropsStrategy
            {
                Settings = TraderSettings.Default
            };
            await CoinTrader.StartAsync();
            CoinTrader.Strategy.Settings.BuyLowerPercentage = 0;
            CoinTrader.Strategy.Settings.TradingBudget = 0.0012M;
            CoinTrader.Strategy.Settings.MinimumTakeProfit = 0M;
            CoinTrader.Strategy.Settings.HighStopLossPercentage = -0.001M;
            CoinTrader.Strategy.Settings.StopLoss = -15;
            CoinTrader.Strategy.Settings.BuyTrigger = -43M;
            CoinTrader.Strategy.Settings.ExpirationTime = TimeSpan.FromHours(2);
            await fakeBittrexApi.SendMarketUpdates(Market);
            Console.WriteLine($"Profit: {CoinTrader.Budget.Profit}%");
            return CoinTrader;
        }
    }
}
