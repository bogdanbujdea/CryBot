using CryBot.Core.Models;
using CryBot.Core.Services;

using FluentAssertions;

using Moq;

using Orleans;

using System.Threading.Tasks;
using System.Collections.Generic;
using CryBot.Core.Models.Grains;
using Xunit;

namespace CryBot.UnitTests.Services
{
    public class TradersManagerTests
    {
        private readonly Mock<ICryptoApi> _cryptoApiMock;
        private readonly TradersManager _tradersManager;
        private readonly Mock<IClusterClient> _clusterClientMock;
        private readonly Mock<ITraderGrain> _initializedTraderGrainMock;
        private readonly Mock<ITraderGrain> _notInitializedTraderGrainMock;

        public TradersManagerTests()
        {
            _cryptoApiMock = new Mock<ICryptoApi>();
            _cryptoApiMock.Setup(c => c.GetMarketsAsync()).ReturnsAsync(new CryptoResponse<List<Market>>(new List<Market>()));
            _clusterClientMock = new Mock<IClusterClient>();
            _initializedTraderGrainMock = new Mock<ITraderGrain>();
            _notInitializedTraderGrainMock = new Mock<ITraderGrain>();
            _initializedTraderGrainMock.Setup(t => t.IsInitialized()).ReturnsAsync(true);
            _notInitializedTraderGrainMock.Setup(t => t.IsInitialized()).ReturnsAsync(false);
            _clusterClientMock.Setup(c => c.GetGrain<ITraderGrain>(It.Is<string>(t => t == "BTC-ETC"), It.IsAny<string>())).Returns(_initializedTraderGrainMock.Object);
            _clusterClientMock.Setup(c => c.GetGrain<ITraderGrain>(It.Is<string>(t => t == "BTC-ETH"), It.IsAny<string>())).Returns(_notInitializedTraderGrainMock.Object);
            _initializedTraderGrainMock.Setup(c => c.GetTraderData()).ReturnsAsync(new TraderState());
            _tradersManager = new TradersManager(_cryptoApiMock.Object, _clusterClientMock.Object, null, null);
        }

        [Fact]
        public async Task GetAllTraders_Should_RetrieveMarketsFromCryptoApi()
        {
            await _tradersManager.GetAllTraders();
            _cryptoApiMock.Verify(c => c.GetMarketsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllTraders_Should_RetrieveActiveGrainsForMarkets()
        {
            _cryptoApiMock.Setup(c => c.GetMarketsAsync()).ReturnsAsync(new CryptoResponse<List<Market>>(new List<Market>
            {
                new Market
                {
                    Name = "BTC-ETC"
                }
            }));
            await _tradersManager.GetAllTraders();
            _clusterClientMock.Verify(c => c.GetGrain<ITraderGrain>(It.Is<string>(s => s == "BTC-ETC"), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GetAllTraders_ShouldReturn_ListOfGrains()
        {
            _cryptoApiMock.Setup(c => c.GetMarketsAsync()).ReturnsAsync(new CryptoResponse<List<Market>>(new List<Market>
            {
                new Market
                {
                    Name = "BTC-ETC"
                },
                new Market
                {
                    Name = "BTC-ETH"
                }

            }));
            var traders = await _tradersManager.GetAllTraders();
            traders.Count.Should().Be(1);
        }

        [Fact]
        public async Task GetAllTraders_ShouldReturn_OnlyGrainsThatAreActive()
        {
            
            _cryptoApiMock.Setup(c => c.GetMarketsAsync()).ReturnsAsync(new CryptoResponse<List<Market>>(new List<Market>
            {
                new Market
                {
                    Name = "BTC-ETC"
                },
                new Market
                {
                    Name = "BTC-ETH"
                }
            }));
            var traders = await _tradersManager.GetAllTraders();
            traders.Count.Should().Be(1);
        }
    }
}
