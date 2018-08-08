using CryBot.Core.Models;
using CryBot.Core.Services;

using FluentAssertions;

using System;

using Xunit;

namespace CryBot.UnitTests.Strategies
{
    public class HoldUntilPriceDropsStrategyTests
    {
        private readonly HoldUntilPriceDropsStrategy _strategy;
        private Trade _currentTrade;

        public HoldUntilPriceDropsStrategyTests()
        {
            _strategy = new HoldUntilPriceDropsStrategy { Settings = TraderSettings.Default };
            _strategy.Settings.HighStopLossPercentage = -5;
            _strategy.Settings.StopLoss = -2;
            _strategy.Settings.BuyTrigger = -1;
            _currentTrade = new Trade
            {
                BuyOrder = new CryptoOrder
                {
                    PricePerUnit = 100,
                    Uuid = "abc"
                }
            };
        }

        [Fact]
        public void MinimumHighStopLoss_Should_ReturnSellAdvice()
        {
            _currentTrade.Status = TradeStatus.Bought;
            _strategy.CalculateTradeAction(new Ticker { Bid = 110 }, _currentTrade).TradeAdvice.Should().Be(TradeAdvice.Hold);
            _strategy.CalculateTradeAction(new Ticker { Bid = 104 }, _currentTrade).TradeAdvice.Should().Be(TradeAdvice.Sell);
        }

        [Fact]
        public void MinimumHighStopLoss_Should_SellImmediately()
        {
            _currentTrade.Status = TradeStatus.Bought;
            _strategy.CalculateTradeAction(new Ticker { Bid = 110 }, _currentTrade).TradeAdvice.Should().Be(TradeAdvice.Hold);
            var tradeAction = _strategy.CalculateTradeAction(new Ticker { Bid = 104 }, _currentTrade);
            tradeAction.OrderPricePerUnit.Should().Be(104);
        }

        [Fact]
        public void MinimumStopLoss_Should_ReturnSellAdvice()
        {
            _strategy.CalculateTradeAction(new Ticker { Bid = 99 }, _currentTrade);
            var tradeAction = _strategy.CalculateTradeAction(new Ticker { Bid = 98 }, _currentTrade);
            tradeAction.TradeAdvice.Should().Be(TradeAdvice.Sell);
        }

        [Fact]
        public void MinimumStopLoss_Should_SellImmediately()
        {
            _strategy.CalculateTradeAction(new Ticker { Bid = 99 }, _currentTrade);
            var tradeAction = _strategy.CalculateTradeAction(new Ticker { Bid = 98 }, _currentTrade);
            tradeAction.TradeAdvice.Should().Be(TradeAdvice.Sell);
            tradeAction.OrderPricePerUnit.Should().Be(98);
        }

        [Fact]
        public void BuyTrigger_Should_ReturnBuyAdvice()
        {
            _strategy.Settings.BuyTrigger = -1;
            var tradeAction = _strategy.CalculateTradeAction(new Ticker { Bid = 98 }, _currentTrade);
            tradeAction.TradeAdvice.Should().Be(TradeAdvice.Buy);
        }

        [Fact]
        public void BuyTrigger_Should_BuyOnBidPrice()
        {
            _strategy.Settings.BuyTrigger = -1;
            var tradeAction = _strategy.CalculateTradeAction(new Ticker { Bid = 98 }, _currentTrade);
            tradeAction.TradeAdvice.Should().Be(TradeAdvice.Buy);
            tradeAction.OrderPricePerUnit.Should().Be(98);
        }

        [Fact]
        public void BuyTrigger_ShouldTrigger_OnlyOncePerTrade()
        {
            _strategy.Settings.BuyTrigger = -1;
            _strategy.Settings.StopLoss = -10;
            _currentTrade.TriggeredBuy = false;
            var tradeAction = _strategy.CalculateTradeAction(new Ticker { Bid = 98 }, _currentTrade);

            _currentTrade.TriggeredBuy.Should().Be(true);
            tradeAction.TradeAdvice.Should().Be(TradeAdvice.Buy);

            tradeAction = _strategy.CalculateTradeAction(new Ticker { Bid = 97 }, _currentTrade);
            tradeAction.TradeAdvice.Should().Be(TradeAdvice.Hold);
        }

        [Fact]
        public void EmptyTrade_Should_ReturnBuyAdvice()
        {
            var tradeAction = _strategy.CalculateTradeAction(new Ticker { Bid = 98, Ask = 100 }, new Trade { Status = TradeStatus.Empty });
            tradeAction.TradeAdvice.Should().Be(TradeAdvice.Buy);
            tradeAction.Reason.Should().Be(TradeReason.FirstTrade);
            tradeAction.OrderPricePerUnit.Should().Be(98);
        }

        [Fact]
        public void PendingOrder_Should_ReturnHold()
        {
            var tradeAction = _strategy.CalculateTradeAction(new Ticker(), new Trade { Status = TradeStatus.Buying });
            tradeAction.TradeAdvice.Should().Be(TradeAdvice.Hold);

            tradeAction = _strategy.CalculateTradeAction(new Ticker(), new Trade { Status = TradeStatus.Selling });
            tradeAction.TradeAdvice.Should().Be(TradeAdvice.Hold);
        }

        [Fact]
        public void ExpiredOrder_Should_ReturnCancelAdvice()
        {
            var trade = new Trade
            {
                Status = TradeStatus.Buying,
                BuyOrder = new CryptoOrder
                {
                    OrderType = CryptoOrderType.LimitBuy,
                    Opened = DateTime.UtcNow,
                    IsOpened = true
                }
            };
            var tradeAction = _strategy.CalculateTradeAction(new Ticker { Timestamp = DateTime.UtcNow.AddHours(6) }, trade);
            tradeAction.TradeAdvice.Should().Be(TradeAdvice.Cancel);
        }
    }
}
