using CryBot.Contracts;
using CryBot.Core.Services;

using FluentAssertions;

using Xunit;

namespace CryBot.UnitTests.Services
{
    public class HoldUntilPriceDropsStrategyTests
    {
        private readonly HoldUntilPriceDropsStrategy _strategy;

        public HoldUntilPriceDropsStrategyTests()
        {
            _strategy = new HoldUntilPriceDropsStrategy { Settings = TraderSettings.Default };
            _strategy.Settings.HighStopLossPercentage = -5;
            _strategy.Settings.StopLoss = -2;
            _strategy.Settings.BuyTrigger = -1;
            _strategy.CurrentTrade = new Trade
            {
                BuyOrder = new CryptoOrder
                {
                    PricePerUnit = 100
                }
            };
        }

        [Fact]
        public void MinimumHighStopLoss_Should_ReturnSellAdvice()
        {
            _strategy.CalculateTradeAction(new Ticker { Bid = 110 }).TradeAdvice.Should().Be(TradeAdvice.Hold);
            _strategy.CalculateTradeAction(new Ticker { Bid = 104 }).TradeAdvice.Should().Be(TradeAdvice.Sell);
        }

        [Fact]
        public void MinimumHighStopLoss_Should_SellImmediately()
        {
            _strategy.CalculateTradeAction(new Ticker { Bid = 110 }).TradeAdvice.Should().Be(TradeAdvice.Hold);
            var tradeAction = _strategy.CalculateTradeAction(new Ticker { Bid = 104 });
            tradeAction.OrderPricePerUnit.Should().Be(104);
        }

        [Fact]
        public void MinimumStopLoss_Should_ReturnSellAdvice()
        {
            _strategy.CalculateTradeAction(new Ticker { Bid = 99 });
            var tradeAction = _strategy.CalculateTradeAction(new Ticker { Bid = 98 });
            tradeAction.TradeAdvice.Should().Be(TradeAdvice.Sell);
        }

        [Fact]
        public void MinimumStopLoss_Should_SellImmediately()
        {
            _strategy.CalculateTradeAction(new Ticker { Bid = 99 });
            var tradeAction = _strategy.CalculateTradeAction(new Ticker { Bid = 98 });
            tradeAction.TradeAdvice.Should().Be(TradeAdvice.Sell);
            tradeAction.OrderPricePerUnit.Should().Be(98);
        }

        [Fact]
        public void BuyTrigger_Should_ReturnBuyAdvice()
        {
            _strategy.Settings.BuyTrigger = -1;
            var tradeAction = _strategy.CalculateTradeAction(new Ticker { Bid = 98 });
            tradeAction.TradeAdvice.Should().Be(TradeAdvice.Buy);
        }

        [Fact]
        public void BuyTrigger_Should_BuyOnBidPrice()
        {
            _strategy.Settings.BuyTrigger = -1;
            var tradeAction = _strategy.CalculateTradeAction(new Ticker { Bid = 98 });
            tradeAction.TradeAdvice.Should().Be(TradeAdvice.Buy);
            tradeAction.OrderPricePerUnit.Should().Be(98);
        }

        [Fact]
        public void BuyTrigger_ShouldTrigger_OnlyOncePerTrade()
        {
            _strategy.Settings.BuyTrigger = -1;
            _strategy.Settings.StopLoss = -10;
            _strategy.CurrentTrade.TriggeredBuy = false;
            var tradeAction = _strategy.CalculateTradeAction(new Ticker { Bid = 98 });

            _strategy.CurrentTrade.TriggeredBuy.Should().Be(true);
            tradeAction.TradeAdvice.Should().Be(TradeAdvice.Buy);

            tradeAction = _strategy.CalculateTradeAction(new Ticker { Bid = 97 });
            tradeAction.TradeAdvice.Should().Be(TradeAdvice.Hold);
        }

        [Fact]
        public void BuyTriggerSmallerThanStopLoss_Should_ReturnHold()
        {
            _strategy.Settings.BuyTrigger = -10;
            var tradeAction = _strategy.CalculateTradeAction(new Ticker { Bid = 98 });
            tradeAction.TradeAdvice.Should().Be(TradeAdvice.Hold);
        }
    }
}
