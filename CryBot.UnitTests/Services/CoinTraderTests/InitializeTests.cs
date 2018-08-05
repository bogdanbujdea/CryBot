using System.Reactive.Subjects;
using FluentAssertions;

using System.Threading.Tasks;
using CryBot.Core.Models;
using CryBot.UnitTests.Infrastructure;
using Moq;
using Xunit;

namespace CryBot.UnitTests.Services.CoinTraderTests
{
    public class InitializeTests : CoinTraderTestBase
    {
        [Fact]
        public void CoinTrader_ShouldBe_InitializedWithAMarket()
        {
            CoinTrader.Initialize("BTC-XLM");
            CoinTrader.Market.Should().Be("BTC-XLM");
        }

        [Fact]
        public void Initialize_ShouldSet_TraderData()
        {
            CoinTrader.Initialize("BTC-XLM");
            CoinTrader.Strategy.Should().NotBeNull();
            CoinTrader.Trades.Should().NotBeNull();
        }

        [Fact]
        public async Task TraderWithNoTrades_Should_CreateFirstTrade()
        {
            CoinTrader.Initialize("BTC-XLM");
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
    }
}
