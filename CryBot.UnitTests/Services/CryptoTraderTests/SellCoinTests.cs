using CryBot.Core.Models;
using CryBot.Core.Services;

using Moq;

using System.Threading.Tasks;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace CryBot.UnitTests.Services.CryptoTraderTests
{
    public class SellCoinTests
    {
        private readonly Mock<ICryptoApi> _cryptoApiMock;
        private CryptoTrader _cryptoTrader;
        private Ticker _ticker;
        private Trade _currentTrade;

        public SellCoinTests()
        {
            _cryptoApiMock = new Mock<ICryptoApi>();
            _cryptoTrader = new CryptoTrader(_cryptoApiMock.Object);
            _cryptoApiMock.Setup(c => c.BuyCoinAsync(It.IsAny<CryptoOrder>())).ReturnsAsync(new CryptoResponse<CryptoOrder>("hello"));
            _cryptoApiMock.Setup(c => c.SellCoinAsync(It.IsAny<CryptoOrder>())).ReturnsAsync(new CryptoResponse<CryptoOrder>("error"));

            _cryptoTrader.Market = "BTC-XLM";
            _currentTrade = new Trade
            {
                BuyOrder = new CryptoOrder
                {
                    PricePerUnit = 100M
                }
            };
            _cryptoTrader.Trades.Add(_currentTrade);
            _cryptoTrader.Settings = new TraderSettings
            {
                HighStopLoss = 5,
                HighStopLossPercentage = -10,
                StopLoss = -2,
                MinimumTakeProfit = 2
            };
        }

        [Fact]
        public async Task MinimumHighLoss_Should_SellCoin()
        {
            await _cryptoTrader.StartAsync();
            RaiseMarketUpdate(110);
            RaiseMarketUpdate(107);
            _cryptoApiMock.Verify(c => c.SellCoinAsync(It.IsAny<CryptoOrder>()), Times.Once);
        }

        [Fact]
        public async Task StopLossTriggered_Should_SellCoin()
        {
            await _cryptoTrader.StartAsync();
            RaiseMarketUpdate(98);
            _cryptoApiMock.Verify(c => c.SellCoinAsync(It.Is<CryptoOrder>(o => o.PricePerUnit == 98)), Times.Once);
        }

        [Fact]
        public async Task SellOrder_Should_BeValid()
        {
            await _cryptoTrader.StartAsync();
            _currentTrade.BuyOrder.Quantity = 100;
            _currentTrade.BuyOrder.Market = _cryptoTrader.Market;
            RaiseMarketUpdate(98);
            _cryptoApiMock.Verify(c => c.SellCoinAsync(It.Is<CryptoOrder>(o => o.PricePerUnit == 98 &&
                                                                               o.Quantity == _currentTrade.BuyOrder.Quantity &&
                                                                               o.Price == 98 * _currentTrade.BuyOrder.Quantity &&
                                                                               o.Market == _currentTrade.BuyOrder.Market)), Times.Once);
        }

        [Fact]
        public async Task MarketUpdate_Should_UpdateAllActiveTrades()
        {
            await _cryptoTrader.StartAsync();
            _cryptoTrader.Trades = new List<Trade>
            {
                new Trade{IsActive = true, MaxPricePerUnit = 10, BuyOrder = new CryptoOrder()},
                new Trade{IsActive = true, MaxPricePerUnit = 10, BuyOrder = new CryptoOrder()},
                new Trade{IsActive = false, MaxPricePerUnit = 10, BuyOrder = new CryptoOrder()}
            };
            RaiseMarketUpdate(100);
            _cryptoTrader.Trades[0].MaxPricePerUnit.Should().Be(100);
            _cryptoTrader.Trades[1].MaxPricePerUnit.Should().Be(100);
            _cryptoTrader.Trades[2].MaxPricePerUnit.Should().Be(10);
        }

        private void RaiseMarketUpdate(decimal last)
        {
            _ticker = new Ticker
            {
                Market = "BTC-XLM",
                Last = last,
                BaseVolume = 1000,
                Ask = 10,
                Bid = 5
            };
            _cryptoApiMock.Raise(c => c.MarketsUpdated += null, _cryptoTrader, new List<Ticker>
            {
                _ticker
            });
        }
    }
}
