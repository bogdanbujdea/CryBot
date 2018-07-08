using CryBot.Contracts;
using CryBot.Core.Models;
using CryBot.Core.Services;

using FluentAssertions;

using Moq;

using Orleans;

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
        private Mock<IClusterClient> _clusterClientMock;
        private Mock<ITraderGrain> _traderGrainMock;
        private Mock<IHubNotifier> _hubNotifier;

        public BuyCoinTests()
        {
            _cryptoApiMock = new Mock<ICryptoApi>();
            _cryptoApiMock.Setup(c => c.BuyCoinAsync(It.IsAny<CryptoOrder>())).ReturnsAsync(new CryptoResponse<CryptoOrder>(new CryptoOrder()));
            CreateDefaultSetups();
        }

        [Fact]
        public async Task TraderWithNoCoin_Should_CreateTwoBuyTrades()
        {
            await _cryptoTrader.StartAsync("");
            _cryptoApiMock.Verify(c => c.BuyCoinAsync(It.IsAny<CryptoOrder>()), Times.Exactly(2));
        }

        [Fact]
        public async Task TraderWithNoCoin_Should_BuyAtCurrentPriceAnd2pLower()
        {
            await _cryptoTrader.StartAsync("");
            _cryptoApiMock.Verify(c => c.BuyCoinAsync(It.Is<CryptoOrder>(order => order.PricePerUnit == 100)), Times.Once);
            _cryptoApiMock.Verify(c => c.BuyCoinAsync(It.Is<CryptoOrder>(order => order.PricePerUnit == 98)), Times.Once);
        }

        [Fact]
        public async Task BeforeMakingATrade_TraderShould_GetTickerInfo()
        {
            await _cryptoTrader.StartAsync("");
            _cryptoApiMock.Verify(c => c.GetTickerAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task TraderWithNoCoin_Should_UseDefaultBudget()
        {
            await _cryptoTrader.StartAsync("");
            _cryptoApiMock.Verify(c => c.BuyCoinAsync(It.Is<CryptoOrder>(o => o.Price == _cryptoTrader.Settings.DefaultBudget)), Times.Exactly(2));
        }

        [Fact]
        public async Task TraderWithTrades_ShouldNotBuy()
        {
            _traderGrainMock.Setup(t => t.GetActiveTrades()).ReturnsAsync(new List<Trade>
            {
                new Trade()
            });
            await _cryptoTrader.StartAsync("");
            _cryptoApiMock.Verify(c => c.BuyCoinAsync(It.IsAny<CryptoOrder>()), Times.Never);
        }

        [Fact]
        public async Task TraderPrice_Should_BeSubscribedToUpdates()
        {
            await _cryptoTrader.StartAsync("BTC-XLM");
            await _cryptoTrader.UpdatePrice(new Ticker
            {
                Market = "BTC-XLM",
                Last = 100,
                BaseVolume = 1000,
                Ask = 10,
                Bid = 5
            });
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
            _cryptoApiMock.Setup(c => c.GetTickerAsync(It.IsAny<string>()))
                .ReturnsAsync(new CryptoResponse<Ticker>(new Ticker
                {
                    Ask = 15,
                    Bid = 100
                }));
            await _cryptoTrader.StartAsync("BTC-XLM");
            await _cryptoTrader.UpdatePrice(new Ticker { Ask = 15 });
            _cryptoApiMock.Raise(c => c.MarketsUpdated += null, _cryptoTrader, new List<Ticker>
            {
                new Ticker
                {
                    Market = "BTC-ETC",
                    Ask = 100
                }
            });
            _cryptoTrader.Ticker.Ask.Should().Be(15);
        }

        [Fact]
        public async Task SecondBuy_ShouldBe_At2pFromBid()
        {
            _cryptoApiMock.Setup(c => c.GetTickerAsync(It.IsAny<string>()))
                .ReturnsAsync(new CryptoResponse<Ticker>(new Ticker
                {
                    Ask = 15,
                    Bid = 100
                }));
            await _cryptoTrader.StartAsync("BTC-XLM");
            _cryptoApiMock.Verify(c => c.BuyCoinAsync(It.Is<CryptoOrder>(o => o.PricePerUnit == 98)), Times.Exactly(1));
        }

        [Fact]
        public async Task BuyingCoin_Should_AddTNewAInactiveTrades()
        {
            await _cryptoTrader.StartAsync("");
            _cryptoTrader.Trades.Count.Should().Be(2);
            _cryptoTrader.Trades[0].IsActive.Should().BeFalse();
            _cryptoTrader.Trades[1].IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task BuyingCoin_Should_AddBuyOrderToTrade()
        {
            await _cryptoTrader.StartAsync("");
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
            _traderGrainMock.Setup(t => t.GetActiveTrades()).ReturnsAsync(new List<Trade>
            {
                new Trade
                {
                    BuyOrder = new CryptoOrder { Uuid = "s2" }
                }
            });
            await _cryptoTrader.StartAsync("");
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
            _clusterClientMock = new Mock<IClusterClient>();
            _hubNotifier = new Mock<IHubNotifier>();
            _cryptoTrader = new CryptoTrader(_cryptoApiMock.Object, _clusterClientMock.Object, _hubNotifier.Object)
            {
                
            };
            _traderGrainMock = new Mock<ITraderGrain>();
            _traderGrainMock.Setup(t => t.UpdatePriceAsync(It.IsAny<Ticker>())).Returns(Task.CompletedTask);
            _clusterClientMock.Setup(c => c.GetGrain<ITraderGrain>(It.IsAny<string>(), It.IsAny<string>())).Returns(_traderGrainMock.Object);
            _traderGrainMock.Setup(t => t.GetActiveTrades()).ReturnsAsync(new List<Trade>());

            _traderGrainMock.Setup(t => t.GetSettings()).ReturnsAsync(new TraderSettings
            {
                BuyLowerPercentage = -2,
                DefaultBudget = 0.0012M
            });
            _cryptoApiMock.Setup(c => c.GetTickerAsync(It.IsAny<string>()))
                .ReturnsAsync(new CryptoResponse<Ticker>(new Ticker
                {
                    Ask = 100,
                    Bid = 100,
                    Last = 100
                }));
        }
    }
}
