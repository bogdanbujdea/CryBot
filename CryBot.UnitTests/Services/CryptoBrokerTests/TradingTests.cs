using CryBot.Core.Exchange.Models;
using CryBot.Core.Strategies;
using CryBot.Core.Trader;
using CryBot.UnitTests.Infrastructure;
using FluentAssertions;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace CryBot.UnitTests.Services.CryptoBrokerTests
{
    public class TradingTests : CoinTraderTestBase
    {
        private readonly Ticker _newPriceTicker = new Ticker { Ask = 100, Bid = 120 };

        public TradingTests()
        {
            Reset();
        }

        [Fact]
        public async Task CoinTrader_Should_BeAbleToReceivePriceUpdates()
        {
            InitializeTrader(new TradeAction { TradeAdvice = TradeAdvice.Sell });

            await CryptoBroker.UpdatePrice(_newPriceTicker);

            CryptoBroker.Ticker.Ask.Should().Be(100);
        }

        [Fact]
        public async Task PriceUpdate_Should_UpdateTraderProfit()
        {
            CryptoApiMock.MockBuyingTrade(new CryptoOrder());
            var trade = new Trade();
            trade.BuyOrder.PricePerUnit = 100;
            trade.BuyOrder.IsClosed = true;
            CryptoBroker.TraderState.Trades = new List<Trade> { trade };
            trade.Status = TradeStatus.Bought;
            CryptoBroker.Strategy = new HoldUntilPriceDropsStrategy();
            await CryptoBroker.UpdatePrice(_newPriceTicker);

            CryptoBroker.TraderState.Trades[0].Profit.Should().Be(19.4M);
        }

        [Fact]
        public async Task CoinTrader_Should_CallStrategyWhenPriceIsUpdated()
        {
            InitializeTrader(new TradeAction { TradeAdvice = TradeAdvice.Sell });

            await CryptoBroker.UpdatePrice(_newPriceTicker);

            Strategy.Verify(s => s.CalculateTradeAction(It.Is<Ticker>(t => t.Ask == 100), It.IsAny<Trade>()), Times.Once);
        }

        [Fact]
        public async Task BuyAdvice_Should_CreateBuyOrder()
        {
            CryptoApiMock.MockBuyingTrade(new CryptoOrder());
            InitializeTrader(new TradeAction { TradeAdvice = TradeAdvice.Buy, OrderPricePerUnit = 98 });

            await CryptoBroker.UpdatePrice(_newPriceTicker);

            CryptoApiMock.Verify(c => c.BuyCoinAsync(It.Is<CryptoOrder>(b => b.PricePerUnit == 98)), Times.Once);
        }

        [Fact]
        public async Task ClosedOrder_Should_UpdateTradeStatus()
        {
            CryptoApiMock.MockBuyingTrade(new CryptoOrder());
            InitializeTrader(new TradeAction { TradeAdvice = TradeAdvice.Buy, OrderPricePerUnit = 98 });
            await CryptoBroker.UpdatePrice(_newPriceTicker);
            await CryptoBroker.UpdateOrder(new CryptoOrder { IsClosed = false, OrderType = CryptoOrderType.LimitBuy });
            CryptoBroker.TraderState.Trades[0].Status.Should().NotBe(TradeStatus.Bought);
        }

        [Fact]
        public async Task SellAdvice_Should_CreateSellOrder()
        {
            InitializeTrader(new TradeAction { TradeAdvice = TradeAdvice.Sell, OrderPricePerUnit = 120 });

            await CryptoBroker.UpdatePrice(_newPriceTicker);

            CryptoApiMock.Verify(c => c.SellCoinAsync(It.Is<CryptoOrder>(b => b.PricePerUnit == 120)), Times.Once);
        }

        [Fact]
        public async Task CancelAdvice_Should_CancelBuyOrder()
        {
            CryptoApiMock.MockCancelTrade(new CryptoOrder());
            InitializeTrader(new TradeAction { TradeAdvice = TradeAdvice.Cancel });

            CryptoBroker.TraderState.Trades[0].BuyOrder.Uuid = "test";
            await CryptoBroker.UpdatePrice(_newPriceTicker);

            CryptoApiMock.Verify(c => c.CancelOrder(It.Is<string>(s => s == "test")), Times.Once);
        }

        [Fact]
        public async Task UpdatePrice_Should_UpdateAllTrades()
        {
            InitializeTrader(new TradeAction { TradeAdvice = TradeAdvice.Cancel });
            CryptoApiMock.MockCancelTrade(new CryptoOrder());
            CryptoBroker.TraderState.Trades.Add(new Trade());

            await CryptoBroker.UpdatePrice(_newPriceTicker);

            Strategy.Verify(s => s.CalculateTradeAction(It.IsAny<Ticker>(), It.IsAny<Trade>()), Times.Exactly(2));
        }

        [Fact]
        public async Task SellOrder_ShouldNot_CreateEmptyTrade()
        {
            InitializeTrader(new TradeAction { TradeAdvice = TradeAdvice.Sell });

            await CryptoBroker.UpdatePrice(_newPriceTicker);

            CryptoBroker.TraderState.Trades.Count.Should().Be(1);
        }

        [Fact]
        public async Task CompletedTrades_ShouldNot_BeUpdated()
        {
            InitializeTrader(new TradeAction { TradeAdvice = TradeAdvice.Hold });
            CryptoBroker.TraderState.Trades[0].Status = TradeStatus.Completed;
            CryptoBroker.TraderState.Trades.Add(new Trade { Status = TradeStatus.Empty });

            await CryptoBroker.UpdatePrice(_newPriceTicker);

            Strategy.Verify(s => s.CalculateTradeAction(It.IsAny<Ticker>(), It.IsAny<Trade>()), Times.Once);
        }

        [Fact]
        public async Task SoldCoin_Should_UpdateTradeStatus()
        {
            var sellOrder = new CryptoOrder { OrderType = CryptoOrderType.LimitSell, IsClosed = true, Price = 1100, Uuid = "S" };
            var trade = new Trade
            {
                SellOrder = sellOrder
            };

            InitializeTrader(new TradeAction { TradeAdvice = TradeAdvice.Sell });
            CryptoBroker.TraderState.Trades.Add(trade);
            await CryptoBroker.UpdateOrder(sellOrder);

            trade.Status.Should().Be(TradeStatus.Completed);
        }

        [Fact]
        public async Task SellingCoin_Should_UpdateTradeStatusIfOrderIsSuccessful()
        {
            InitializeTrader(new TradeAction { TradeAdvice = TradeAdvice.Sell });

            await CryptoBroker.UpdatePrice(_newPriceTicker);

            CryptoBroker.TraderState.Trades[0].Status.Should().Be(TradeStatus.Selling);
        }

        [Fact]
        public async Task CancellingBuyOrder_Should_RemoveTrade()
        {
            CryptoApiMock.MockCancelTrade(new CryptoOrder());
            InitializeTrader(new TradeAction { TradeAdvice = TradeAdvice.Cancel });

            await CryptoBroker.UpdatePrice(_newPriceTicker);

            CryptoBroker.TraderState.Trades.Count.Should().Be(0);
        }

        [Fact]
        public async Task EmptyTradesList_Should_AddNewTrade()
        {
            CryptoApiMock.MockBuyingTrade(new CryptoOrder());
            InitializeTrader(new TradeAction { TradeAdvice = TradeAdvice.Buy, OrderPricePerUnit = 98 });
            CryptoBroker.TraderState.Trades = new List<Trade>();

            await CryptoBroker.UpdatePrice(_newPriceTicker);

            CryptoBroker.TraderState.Trades.Count.Should().Be(1);
        }


        [Fact]
        public async Task OnlyCompletedTrades_Should_AddNewTrade()
        {
            CryptoApiMock.MockBuyingTrade(new CryptoOrder());
            InitializeTrader(new TradeAction { TradeAdvice = TradeAdvice.Buy, OrderPricePerUnit = 98 });
            CryptoBroker.TraderState.Trades = new List<Trade> { new Trade { Status = TradeStatus.Completed } };

            await CryptoBroker.UpdatePrice(_newPriceTicker);

            CryptoBroker.TraderState.Trades.Count.Should().Be(2);
        }

        [Fact]
        public async Task BoughtOrder_Should_UpdateTraderStatus()
        {
            var trade = new Trade();
            CryptoBroker.TraderState.Trades = new List<Trade> { trade };
            trade.BuyOrder.Uuid = "B";
            var buyOrder = new CryptoOrder
            {
                Uuid = "B",
                OrderType = CryptoOrderType.LimitBuy,
                IsClosed = true
            };

            await CryptoBroker.UpdateOrder(buyOrder);

            CryptoBroker.TraderState.Trades[0].Status.Should().Be(TradeStatus.Bought);
        }

        [Fact]
        public async Task BuyingOrder_Should_UpdateTraderStatus()
        {
            CryptoApiMock.MockBuyingTrade(new CryptoOrder());
            InitializeTrader(new TradeAction { TradeAdvice = TradeAdvice.Buy, OrderPricePerUnit = 98 });

            await CryptoBroker.UpdatePrice(_newPriceTicker);
            CryptoBroker.TraderState.Trades[0].Status.Should().Be(TradeStatus.Buying);
        }

        [Fact]
        public async Task SellingCoin_Should_UpdateOrderForTrade()
        {
            InitializeTrader(new TradeAction { TradeAdvice = TradeAdvice.Sell, OrderPricePerUnit = 98 });
            var sellOrder = new CryptoOrder { OrderType = CryptoOrderType.LimitSell, Price = 1100, Uuid = "S" };
            CryptoApiMock.MockSellingTrade(sellOrder);
            CryptoBroker.TraderState.Trades[0].SellOrder.Uuid = "S";

            await CryptoBroker.UpdatePrice(_newPriceTicker);

            CryptoBroker.TraderState.Trades[0].SellOrder.Uuid.Should().Be("S");
            CryptoBroker.TraderState.Trades[0].SellOrder.Price.Should().Be(1100);
        }
    }
}
