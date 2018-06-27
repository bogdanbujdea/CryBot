using Bittrex.Net.Objects;
using Bittrex.Net.Interfaces;

using CryptoExchange.Net;

using FluentAssertions;

using Moq;

using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

namespace CryBot.UnitTests.Services.BittrexApi
{
    public class WalletTests
    {
        private readonly Mock<IBittrexClient> _bittrexClientMock;
        private readonly Core.Services.BittrexApi _bittrexApi;

        public WalletTests()
        {
            _bittrexClientMock = new Mock<IBittrexClient>();
            _bittrexClientMock.Setup(b => b.GetMarketSummariesAsync())
                .ReturnsAsync(new CallResult<BittrexMarketSummary[]>(new List<BittrexMarketSummary>().ToArray(), null));
            _bittrexApi = new Core.Services.BittrexApi(_bittrexClientMock.Object);
        }

        [Fact]
        public async Task GetWallet_ShouldReturn_ListOfCoins()
        {
            _bittrexClientMock.Setup(b => b.GetBalancesAsync())
                .ReturnsAsync(() => CreateBalanceResult(new List<BittrexBalance>
                {
                    new BittrexBalance
                    {
                        Currency = "XLM",
                        Available = 100,
                        Balance = 200,
                        Pending = 100
                    }
                }, null));
            var walletResponse = await _bittrexApi.GetWalletAsync();
            walletResponse.Content.Should().NotBeNull();
            walletResponse.Content.Coins.Count.Should().Be(1);
            var coin = walletResponse.Content.Coins[0];
            coin.Market.Should().Be("BTC-XLM");
        }

        private CallResult<BittrexBalance[]> CreateBalanceResult(List<BittrexBalance> coins, Error error)
        {
            return new CallResult<BittrexBalance[]>(coins.ToArray(), error);
        }
    }
}
