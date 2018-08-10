using CryBot.Core.Models;
using CryBot.Core.Models.Grains;
using CryBot.UnitTests.Infrastructure;

using FluentAssertions;

using Moq;

using System.Threading.Tasks;
using System.Reactive.Subjects;
using System.Collections.Generic;

using Xunit;

namespace CryBot.UnitTests.Services.CoinTraderTests
{
    public class InitializeTests : CoinTraderTestBase
    {
        [Fact]
        public void CoinTrader_ShouldBe_InitializedWithAMarket()
        {
            CoinTrader.Initialize(Market);
            CoinTrader.Market.Should().Be(Market);
        }

        [Fact]
        public void Initialize_ShouldSet_TraderData()
        {
            CoinTrader.Initialize(Market);
            CoinTrader.Strategy.Should().NotBeNull();
            CoinTrader.Trades.Should().NotBeNull();
        }

        [Fact]
        public async Task TraderWithNoTrades_Should_CreateFirstTrade()
        {
            CoinTrader.Initialize(Market);
            await CoinTrader.StartAsync();
            CoinTrader.Trades.Count.Should().Be(1);
        }

        [Fact]
        public async Task UpdatePrice_ShouldNot_RunConcurrently()
        {
            var subject = new Subject<Ticker>();
            CryptoApiMock.MockBuyingTrade(new CryptoOrder());
            CryptoApiMock.SetupGet(c => c.TickerUpdated).Returns(subject);
            await InitializeTrader(new TradeAction { TradeAdvice = TradeAdvice.Buy, OrderPricePerUnit = 1});

            for (int i = 0; i < 100; i++)
            {
                subject.OnNext(new Ticker { Market = Market });
            }

            Strategy.Verify(s => s.CalculateTradeAction(It.IsAny<Ticker>(), It.IsAny<Trade>()), Times.Exactly(100));
        }

        [Fact]
        public async Task Start_Should_GetTraderData()
        {
            await InitializeTrader(new TradeAction { TradeAdvice = TradeAdvice.Buy, OrderPricePerUnit = 1});
            TraderGrainMock.Verify(o => o.GetTraderData(), Times.Once);
        }

        [Fact]
        public async Task TraderGrain_Should_InitializeTrader()
        {
            TraderGrainMock.Setup(t => t.GetTraderData()).ReturnsAsync(new TraderState
            {
                Trades = new List<Trade>{Trade.Empty, Trade.Empty, Trade.Empty, Trade.Empty}
            });
            await InitializeTrader(new TradeAction { TradeAdvice = TradeAdvice.Buy, OrderPricePerUnit = 1});
            CoinTrader.Trades.Count.Should().Be(4);
            CoinTrader.Strategy.Settings.Should().NotBeNull();
        }
    }
}
