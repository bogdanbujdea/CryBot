using System.Collections.Generic;
using System.Threading.Tasks;
using CryBot.Core.Models;
using CryBot.Core.Services;
using Moq;
using Xunit;

namespace CryBot.UnitTests.Services.CryptoTraderTests
{
    public class BuyCoinTests
    {
        private Mock<ICryptoApi> _cryptoApiMock;
        private CryptoTrader _cryptoTrader;

        public BuyCoinTests()
        {
            _cryptoApiMock = new Mock<ICryptoApi>();
            _cryptoApiMock.Setup(c => c.BuyCoinAsync(It.IsAny<CryptoOrder>())).ReturnsAsync(new CryptoResponse<CryptoOrder>("hello"));
            CreateDefaultSetups();
        }

        [Fact]
        public async Task TraderWithNoCoin_Should_CreateTwoBuyTrades()
        {
            await _cryptoTrader.StartAsync();
            _cryptoApiMock.Verify(c => c.BuyCoinAsync(It.IsAny<CryptoOrder>()), Times.Exactly(2));
        }

        [Fact]
        public async Task TraderWithNoCoin_Should_BuyAtCurrentPriceAnd2pLower()
        {
            await _cryptoTrader.StartAsync();
            _cryptoApiMock.Verify(c => c.BuyCoinAsync(It.Is<CryptoOrder>(order => order.PricePerUnit == 100)), Times.Once);
            _cryptoApiMock.Verify(c => c.BuyCoinAsync(It.Is<CryptoOrder>(order => order.PricePerUnit == 98)), Times.Once);
        }

        [Fact]
        public async Task BeforeMakingATrade_TraderShould_GetTickerInfo()
        {
            await _cryptoTrader.StartAsync();
            _cryptoApiMock.Verify(c => c.GetTickerAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task TraderWithNoCoin_Should_UseDefaultBudget()
        {
            await _cryptoTrader.StartAsync();
            _cryptoApiMock.Verify(c => c.BuyCoinAsync(It.Is<CryptoOrder>(o => o.Price == 0.0012M)), Times.Exactly(2));
        }

        [Fact]
        public async Task TraderWithTrades_ShouldNotBuy()
        {
            _cryptoTrader.Trades.Add(new Trade());
            await _cryptoTrader.StartAsync();
            _cryptoApiMock.Verify(c => c.BuyCoinAsync(It.IsAny<CryptoOrder>()), Times.Never);
        }

        private void CreateDefaultSetups()
        {
            _cryptoTrader = new CryptoTrader(_cryptoApiMock.Object)
            {
                Market = "BTC-XLM",
                Trades = new List<Trade>()
            };

            _cryptoApiMock.Setup(c => c.GetTickerAsync(It.IsAny<string>()))
                .ReturnsAsync(new CryptoResponse<Ticker>(new Ticker
                {
                    Last = 100
                }));
        }
    }
}
