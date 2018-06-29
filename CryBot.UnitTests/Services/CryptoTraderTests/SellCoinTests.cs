using CryBot.Core.Models;
using CryBot.Core.Services;

using FluentAssertions;

using Moq;

using System.Threading.Tasks;
using System.Collections.Generic;
using CryBot.Core.Utilities;
using Xunit;

namespace CryBot.UnitTests.Services.CryptoTraderTests
{
    public class SellCoinTests
    {
        private readonly Mock<ICryptoApi> _cryptoApiMock;
        private CryptoTrader _cryptoTrader;
        private Ticker _ticker;
        private Trade _currentTrade;
        private CryptoOrder _sellOrder;

        public SellCoinTests()
        {
            _cryptoApiMock = new Mock<ICryptoApi>();
            _cryptoTrader = new CryptoTrader(_cryptoApiMock.Object);
            _cryptoApiMock.Setup(c => c.BuyCoinAsync(It.IsAny<CryptoOrder>())).ReturnsAsync(new CryptoResponse<CryptoOrder>("hello"));
            _sellOrder = new CryptoOrder();
            _cryptoApiMock.Setup(c => c.SellCoinAsync(It.IsAny<CryptoOrder>())).ReturnsAsync(new CryptoResponse<CryptoOrder>(_sellOrder));

            _cryptoTrader.Market = "BTC-XLM";
            _currentTrade = new Trade
            {
                BuyOrder = new CryptoOrder
                {
                    PricePerUnit = 100M
                },
                IsActive = true
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

        [Fact]
        public async Task SellingCoin_Should_AddSellOrderToTrade()
        {
            await _cryptoTrader.StartAsync();
            _currentTrade.BuyOrder.Quantity = 100;
            _currentTrade.BuyOrder.Market = _cryptoTrader.Market;
            _sellOrder.Uuid = "test";
            RaiseMarketUpdate(98);
            _currentTrade.SellOrder.Should().NotBeNull();
            _currentTrade.SellOrder.Uuid.Should().Be("test");
        }

        [Fact]
        public async Task SoldOrder_Should_MarkTradeAsInactive()
        {
            _cryptoTrader.Trades = new List<Trade>
            {
                new Trade{IsActive = true, SellOrder = new CryptoOrder{Uuid = "1"}, MaxPricePerUnit = 10, BuyOrder = new CryptoOrder()},
                new Trade{IsActive = true, SellOrder =new CryptoOrder{Uuid = "2"}, MaxPricePerUnit = 10, BuyOrder = new CryptoOrder()}
            };
            await _cryptoTrader.StartAsync();
            RaiseClosedOrder("1");
            _cryptoTrader.Trades[0].IsActive.Should().BeFalse();
            _cryptoTrader.Trades[1].IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task SoldCoin_Should_BuyLower()
        {
            _sellOrder.Uuid = "s2";
            _sellOrder.PricePerUnit = 98;
            _cryptoTrader.Settings.BuyLowerPercentage = -2;
            await _cryptoTrader.StartAsync();
            RaiseMarketUpdate(98);
            RaiseClosedOrder("s2");
            _cryptoApiMock.Verify(c => c.BuyCoinAsync(It.Is<CryptoOrder>(order => order.PricePerUnit == 98 * _cryptoTrader.Settings.BuyLowerPercentage.ToPercentageMultiplier())), Times.Once);
        }

        private void RaiseClosedOrder(string uuid)
        {
            _sellOrder.Uuid = uuid;
            _sellOrder.IsClosed = true;
            _sellOrder.OrderType = CryptoOrderType.LimitSell;
            _cryptoApiMock.Raise(c => c.OrderUpdated += null, _cryptoTrader, _sellOrder);
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
