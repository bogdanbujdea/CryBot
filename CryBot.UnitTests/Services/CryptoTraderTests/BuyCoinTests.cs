using CryBot.Core.Models;
using CryBot.Core.Services;

using FluentAssertions;

using Moq;

using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

namespace CryBot.UnitTests.Services.CryptoTraderTests
{
    public class BuyCoinTests
    {
        private readonly Mock<ICryptoApi> _cryptoApiMock;
        private CryptoTrader _cryptoTrader;
        private CryptoOrder _updatedOrder;

        public BuyCoinTests()
        {
            _cryptoApiMock = new Mock<ICryptoApi>();
            _cryptoApiMock.Setup(c => c.BuyCoinAsync(It.IsAny<CryptoOrder>())).ReturnsAsync(new CryptoResponse<CryptoOrder>(new CryptoOrder()));
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
            _cryptoTrader.Settings.DefaultBudget = 0.0012M;
            await _cryptoTrader.StartAsync();
            _cryptoApiMock.Verify(c => c.BuyCoinAsync(It.Is<CryptoOrder>(o => o.Price == _cryptoTrader.Settings.DefaultBudget)), Times.Exactly(2));
        }

        [Fact]
        public async Task TraderWithTrades_ShouldNotBuy()
        {
            _cryptoTrader.Trades.Add(new Trade());
            await _cryptoTrader.StartAsync();
            _cryptoApiMock.Verify(c => c.BuyCoinAsync(It.IsAny<CryptoOrder>()), Times.Never);
        }

        [Fact]
        public async Task TraderPrice_Should_BeSubscribedToUpdates()
        {
            _cryptoTrader.Market = "BTC-XLM";
            await _cryptoTrader.StartAsync();
            _cryptoApiMock.Raise(c => c.MarketsUpdated += null, _cryptoTrader, new List<Ticker>
            {
                new Ticker
                {
                    Market = "BTC-XLM",
                    Last = 100,
                    BaseVolume = 1000,
                    Ask = 10,
                    Bid = 5
                }
            });
            _cryptoTrader.Ticker.Last.Should().Be(100);
            _cryptoTrader.Ticker.Ask.Should().Be(10);
            _cryptoTrader.Ticker.Bid.Should().Be(5);
            _cryptoTrader.Ticker.BaseVolume.Should().Be(1000);
            _cryptoTrader.Ticker.Market.Should().Be("BTC-XLM");
        }

        [Fact]
        public async Task NotFoundMarket_ShouldNot_UpdateTicker()
        {
            _cryptoTrader.Market = "BTC-XLM";
            _cryptoTrader.Ticker = new Ticker { Ask = 5 };
            _cryptoApiMock.Setup(c => c.GetTickerAsync(It.IsAny<string>()))
                .ReturnsAsync(new CryptoResponse<Ticker>(new Ticker
                {
                    Ask = 15
                }));
            await _cryptoTrader.StartAsync();
            _cryptoApiMock.Raise(c => c.MarketsUpdated += null, _cryptoTrader, new List<Ticker>
            {
                new Ticker
                {
                    Market = "BTC-ETC",
                    Last = 100,
                }
            });
            _cryptoTrader.Ticker.Ask.Should().Be(15);
        }

        [Fact]
        public async Task BuyingCoin_Should_AddTNewActiverade()
        {
            await _cryptoTrader.StartAsync();
            _cryptoTrader.Trades.Count.Should().Be(2);
            _cryptoTrader.Trades[0].IsActive.Should().BeTrue();
            _cryptoTrader.Trades[1].IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task BuyingCoin_Should_AddBuyOrderToTrade()
        {
            await _cryptoTrader.StartAsync();
            _cryptoTrader.Trades[0].BuyOrder.Should().NotBeNull();
        }

        [Fact]
        public async Task FilledBuyOrder_Should_ActivateTrade()
        {
            _updatedOrder = new CryptoOrder
            {
                Uuid = "s2",
                OrderType = CryptoOrderType.LimitBuy
            };
            _cryptoTrader.Trades.Add(new Trade
            {
                BuyOrder = new CryptoOrder { Uuid = "s2" }
            });            
            await _cryptoTrader.StartAsync();
            RaiseClosedOrder("s2");
            _cryptoTrader.Trades[0].BuyOrder.IsClosed.Should().BeTrue();
        }

        private void RaiseClosedOrder(string uuid)
        {
            _updatedOrder.Uuid = uuid;
            _updatedOrder.IsClosed = true;
            _cryptoApiMock.Raise(c => c.OrderUpdated += null, _cryptoTrader, _updatedOrder);
        }
        private void CreateDefaultSetups()
        {
            _cryptoTrader = new CryptoTrader(_cryptoApiMock.Object)
            {
                Market = "BTC-XLM",
                Trades = new List<Trade>(),
                Settings = new TraderSettings
                {
                    BuyLowerPercentage = -2
                }
            };

            _cryptoApiMock.Setup(c => c.GetTickerAsync(It.IsAny<string>()))
                .ReturnsAsync(new CryptoResponse<Ticker>(new Ticker
                {
                    Last = 100
                }));
        }
    }
}
