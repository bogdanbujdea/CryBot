using FluentAssertions;

using System.Threading.Tasks;

using Xunit;

namespace CryBot.UnitTests.Services.CoinTraderTests
{
    public class InitializeTests: CoinTraderTestBase
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
    }
}
