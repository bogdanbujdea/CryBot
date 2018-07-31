using CryBot.Contracts;
using CryBot.Core.Models;
using CryBot.Core.Services;
using CryBot.Core.Utilities;

using FluentAssertions;

using Moq;

using Orleans;

using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

namespace CryBot.UnitTests.Services.CryptoTraderTests
{
    public class SellCoinTests
    {
        private readonly Mock<ICryptoApi> _cryptoApiMock;
        private CryptoTrader _cryptoTrader;
        private Ticker _ticker;
        private Trade _currentTrade;
        private CryptoOrder _updatedOrder;
        private string _defaultMarket = "BTC-XLM";
        private Mock<IClusterClient> _clusterClientMock;
        private Mock<ITraderGrain> _traderGrainMock;
        private Mock<IHubNotifier> _hubNotifier;

        public SellCoinTests()
        {
            _cryptoApiMock = new Mock<ICryptoApi>();
            _clusterClientMock = new Mock<IClusterClient>();
            _hubNotifier = new Mock<IHubNotifier>();
            _cryptoTrader = new CryptoTrader(_cryptoApiMock.Object, _clusterClientMock.Object, _hubNotifier.Object);
            _cryptoApiMock.Setup(c => c.BuyCoinAsync(It.IsAny<CryptoOrder>())).ReturnsAsync(new CryptoResponse<CryptoOrder>("hello"));
            _updatedOrder = new CryptoOrder();
            _cryptoApiMock.Setup(c => c.SellCoinAsync(It.IsAny<CryptoOrder>())).ReturnsAsync(new CryptoResponse<CryptoOrder>(_updatedOrder));

            _currentTrade = new Trade
            {
                BuyOrder = new CryptoOrder
                {
                    PricePerUnit = 100M
                },
                IsActive = true
            };
            
            _cryptoApiMock.Setup(c => c.GetTickerAsync(It.IsAny<string>()))
                .ReturnsAsync(new CryptoResponse<Ticker>(new Ticker
                {
                    Ask = 15,
                    Bid = 100
                }));
            _traderGrainMock = new Mock<ITraderGrain>();
            _clusterClientMock.Setup(c => c.GetGrain<ITraderGrain>(It.IsAny<string>(), It.IsAny<string>())).Returns(_traderGrainMock.Object);
            _traderGrainMock.Setup(t => t.UpdatePriceAsync(It.IsAny<Ticker>())).Returns(Task.CompletedTask);

            _traderGrainMock.Setup(t => t.UpdatePriceAsync(It.IsAny<Ticker>())).Returns(Task.CompletedTask);
            _traderGrainMock.Setup(t => t.GetSettings()).ReturnsAsync(new TraderSettings
            {
                HighStopLossPercentage = -10,
                StopLoss = -2,
                BuyTrigger = -1,
                MinimumTakeProfit = 2
            });

            _traderGrainMock.Setup(t => t.GetActiveTrades()).ReturnsAsync(new List<Trade>
            {
                _currentTrade
            });
        }

        [Fact]
        public async Task MinimumHighLoss_Should_SellCoin()
        {
            await _cryptoTrader.StartAsync(_defaultMarket);
            await RaiseMarketUpdate(110);
            await RaiseMarketUpdate(107);
            _cryptoApiMock.Verify(c => c.SellCoinAsync(It.IsAny<CryptoOrder>()), Times.Once);
        }

        [Fact]
        public async Task StopLossTriggered_Should_SellCoin()
        {
            await _cryptoTrader.StartAsync(_defaultMarket);
            await RaiseMarketUpdate(99);
            await RaiseMarketUpdate(98);
            _cryptoApiMock.Verify(c => c.SellCoinAsync(It.Is<CryptoOrder>(o => o.PricePerUnit == 98)), Times.Once);
        }

        [Fact]
        public async Task SellOrder_Should_BeValid()
        {
            await _cryptoTrader.StartAsync(_defaultMarket);
            _currentTrade.BuyOrder.Quantity = 100;
            _currentTrade.BuyOrder.Market = _cryptoTrader.Market;
            await RaiseMarketUpdate(99);
            await RaiseMarketUpdate(98);
            _cryptoApiMock.Verify(c => c.SellCoinAsync(It.Is<CryptoOrder>(o => o.PricePerUnit == 98 &&
                                                                               o.Quantity == _currentTrade.BuyOrder.Quantity &&
                                                                               o.Price == 98 * _currentTrade.BuyOrder.Quantity &&
                                                                               o.Market == _currentTrade.BuyOrder.Market)), Times.Once);
        }

        [Fact]
        public async Task MarketUpdate_Should_UpdateAllActiveTrades()
        {
            _traderGrainMock.Setup(t => t.GetActiveTrades()).ReturnsAsync(new List<Trade>
            {
                new Trade{ IsActive = true, MaxPricePerUnit = 10, BuyOrder = new CryptoOrder() },
                new Trade{ IsActive = true, MaxPricePerUnit = 10, BuyOrder = new CryptoOrder() },
                new Trade{ IsActive = false, MaxPricePerUnit = 10, BuyOrder = new CryptoOrder() }
            });
            await _cryptoTrader.StartAsync(_defaultMarket);

            await RaiseMarketUpdate(101);
            _cryptoTrader.Trades[0].MaxPricePerUnit.Should().Be(101);
            _cryptoTrader.Trades[1].MaxPricePerUnit.Should().Be(101);
            _cryptoTrader.Trades[2].MaxPricePerUnit.Should().Be(10);
        }

        [Fact]
        public async Task SellingCoin_Should_AddSellOrderToTrade()
        {
            await _cryptoTrader.StartAsync(_defaultMarket);
            _currentTrade.BuyOrder.Quantity = 100;
            _currentTrade.BuyOrder.Market = _cryptoTrader.Market;
            _updatedOrder.Uuid = "test";
            await RaiseMarketUpdate(99);
            await RaiseMarketUpdate(98);
            _currentTrade.SellOrder.Should().NotBeNull();
            _currentTrade.SellOrder.Uuid.Should().Be("test");
        }

        [Fact]
        public async Task SoldOrder_Should_MarkTradeAsInactive()
        {
            var traderGrainMock = new Mock<ITraderGrain>();
            _clusterClientMock.Setup(c => c.GetGrain<ITraderGrain>(It.IsAny<string>(), It.IsAny<string>())).Returns(traderGrainMock.Object);

            traderGrainMock.Setup(t => t.GetActiveTrades()).ReturnsAsync(new List<Trade>
            {
                new Trade{IsActive = true, SellOrder = new CryptoOrder{Uuid = "1"}, MaxPricePerUnit = 10, BuyOrder = new CryptoOrder()},
                new Trade{IsActive = true, SellOrder = new CryptoOrder{Uuid = "2"}, MaxPricePerUnit = 10, BuyOrder = new CryptoOrder()}
            });
            _updatedOrder.Market = "BTC-XLM";
            await _cryptoTrader.StartAsync(_defaultMarket);
            RaiseClosedOrder("1");
            _cryptoTrader.Trades[0].IsActive.Should().BeFalse();
            _cryptoTrader.Trades[1].IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task SoldCoin_Should_BuyLower()
        {
            _updatedOrder.Uuid = "s2";
            _updatedOrder.PricePerUnit = 98;
            _updatedOrder.Market = "BTC-XLM";
            await _cryptoTrader.StartAsync(_defaultMarket);
            _cryptoTrader.Settings.BuyLowerPercentage = -2;
            await RaiseMarketUpdate(99);
            await RaiseMarketUpdate(98);
            RaiseClosedOrder("s2");
            _cryptoApiMock
                .Verify(c => c.BuyCoinAsync(It.Is<CryptoOrder>(order => 
                    order.PricePerUnit == 98 * _cryptoTrader.Settings.BuyLowerPercentage.ToPercentageMultiplier())), Times.Once);
        }

        private void RaiseClosedOrder(string uuid)
        {
            _updatedOrder.Uuid = uuid;
            _updatedOrder.IsClosed = true;
            _updatedOrder.OrderType = CryptoOrderType.LimitSell;
            _cryptoApiMock.Raise(c => c.OrderUpdated += null, _cryptoTrader, _updatedOrder);
        }

        private async Task RaiseMarketUpdate(decimal last)
        {
            _ticker = new Ticker
            {
                Market = _defaultMarket,
                Last = last,
                BaseVolume = 1000,
                Ask = 10,
                Bid = last
            };
            _cryptoApiMock.Raise(c => c.MarketsUpdated += null, _cryptoTrader, new List<Ticker>
            {
                _ticker
            });
            await _cryptoTrader.ProcessMarketUpdates();
        }
    }
}
