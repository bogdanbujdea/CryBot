using Bittrex.Net.Objects;

using CryBot.Core.Models;
using CryBot.Core.Utilities;

using FluentAssertions;

using System;

using Xunit;

namespace CryBot.UnitTests.Utilities
{
    public class TradingExtensionsTests
    {
        [Fact]
        public void ToCoinBalance_Should_Convert()
        {
            var bittrexBalance = new BittrexBalance
            {
                Currency = "XLM",
                Available = 50,
                Balance = 100,
                Pending = 50
            };
            var coinBalance = bittrexBalance.ConvertToCoinBalance();
            coinBalance.Market.Should().Be("BTC-XLM");
            coinBalance.Quantity.Should().Be(100);
            coinBalance.Available.Should().Be(50);
        }


        [Fact]
        public void ToCryptoOrder_Should_Convert()
        {
            var bittrexOrder = new BittrexOpenOrdersOrder
            {
                Exchange = "BTC-XLM",
                OrderType = OrderSideExtended.LimitBuy,
                Price = 0.001M,
                Quantity = 100,
                PricePerUnit = 0.0001M,
                Limit = 0.0001M,
                CommissionPaid = 0.00001M,
                Closed = DateTime.MaxValue,
                Opened = DateTime.MaxValue,
                Uuid = Guid.NewGuid(),
                QuantityRemaining = 0.002M
            };
            var order = bittrexOrder.ToCryptoOrder();

            order.Market.Should().Be(bittrexOrder.Exchange);
            order.OrderType.Should().Be(CryptoOrderType.LimitBuy);
            order.Price.Should().Be(bittrexOrder.Price);
            order.Quantity.Should().Be(bittrexOrder.Quantity);
            order.QuantityRemaining.Should().Be(bittrexOrder.QuantityRemaining);
            order.PricePerUnit.Should().Be(bittrexOrder.PricePerUnit);
            order.Uuid.Should().Be(bittrexOrder.Uuid.GetValueOrDefault().ToString());
            order.CommissionPaid.Should().Be(bittrexOrder.CommissionPaid);
            order.Limit.Should().Be(bittrexOrder.Limit);
            order.Opened.Should().Be(bittrexOrder.Opened);
            order.IsClosed.Should().BeFalse();
            order.Closed.Should().Be(bittrexOrder.Closed.GetValueOrDefault());
        }

        [Fact]
        public void ToCryptoOrder_Should_ConvertFromHistoricOrder()
        {
            var bittrexOrder = new BittrexOrderHistoryOrder
            {
                Exchange = "BTC-XLM",
                OrderType = OrderSideExtended.LimitBuy,
                Price = 0.001M,
                Quantity = 100,
                PricePerUnit = 0.0001M,
                Limit = 0.0001M,
                Commission = 0.00001M,
                Closed = DateTime.MaxValue,
                TimeStamp = DateTime.MinValue,
                OrderUuid = Guid.NewGuid(),
                QuantityRemaining = 0.002M,
            };
            var order = bittrexOrder.ToCryptoOrder();

            order.Market.Should().Be(bittrexOrder.Exchange);
            order.OrderType.Should().Be(CryptoOrderType.LimitBuy);
            order.Price.Should().Be(bittrexOrder.Price);
            order.Quantity.Should().Be(bittrexOrder.Quantity);
            order.QuantityRemaining.Should().Be(bittrexOrder.QuantityRemaining);
            order.PricePerUnit.Should().Be(bittrexOrder.PricePerUnit);
            order.Uuid.Should().Be(bittrexOrder.OrderUuid.ToString());
            order.CommissionPaid.Should().Be(bittrexOrder.Commission);
            order.Limit.Should().Be(bittrexOrder.Limit);
            order.Opened.Should().Be(bittrexOrder.TimeStamp);
            order.IsClosed.Should().BeTrue();
            order.Closed.Should().Be(bittrexOrder.Closed.GetValueOrDefault());
        }

        [Fact]
        public void MinimumHighStopLoss_ShouldBe_HigherThanMinPrice()
        {
            var currentPrice = 105M;
            var maxPrice = 110M;
            var minimumLimit = 104M;
            var percentage = 0.9M;
            decimal buyPrice = 100;
            var highhStopLossReached = currentPrice.ReachedHighStopLoss(maxPrice, minimumLimit, percentage, buyPrice);
            highhStopLossReached.Should().BeTrue();
        }

        [Fact]
        public void GoingLowAboveMinimumPercentage_ShouldNotTrigger_HighStopLoss()
        {
            var currentPrice = 108M;
            var maxPrice = 110M;
            var minimumLimit = 105M;
            var percentage = 0.7M;
            decimal buyPrice = 100;
            var highhStopLossReached = currentPrice.ReachedHighStopLoss(maxPrice, minimumLimit, percentage, buyPrice);
            highhStopLossReached.Should().BeFalse();
        }

        [Fact]
        public void GoingLowerThanMinimumLimit_ShouldNotTriggerHighStopLoss()
        {
            var currentPrice = 104M;
            var maxPrice = 110M;
            var minimumLimit = 105M;
            var percentage = 0.7M;
            decimal buyPrice = 100;
            var highhStopLossReached = currentPrice.ReachedHighStopLoss(maxPrice, minimumLimit, percentage, buyPrice);
            highhStopLossReached.Should().BeFalse();
        }

        [Fact]
        public void GoingLowerThanPercentageAndHigherThanMinimumLimit_ShouldTriggerHighStopLoss()
        {
            var currentPrice = 106M;
            var maxPrice = 110M;
            var minimumLimit = 105M;
            var percentage = 0.9M;
            decimal buyPrice = 100;
            var highhStopLossReached = currentPrice.ReachedHighStopLoss(maxPrice, minimumLimit, percentage, buyPrice);
            highhStopLossReached.Should().BeTrue();
        }

        [Fact]
        public void PercentageMultiplierTests()
        {
            1M.ToPercentageMultiplier().Should().Be(1.01M);
            (-1M).ToPercentageMultiplier().Should().Be(0.99M);
            (-11M).ToPercentageMultiplier().Should().Be(0.89M);
            0.5M.ToPercentageMultiplier().Should().Be(1.005M);
            0.25M.ToPercentageMultiplier().Should().Be(1.0025M);
        }
    }
}
