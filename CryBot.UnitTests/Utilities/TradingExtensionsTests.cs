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
            order.Uuid.Should().Be(bittrexOrder.Uuid.GetValueOrDefault());
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
            order.Uuid.Should().Be(bittrexOrder.OrderUuid);
            order.CommissionPaid.Should().Be(bittrexOrder.Commission);
            order.Limit.Should().Be(bittrexOrder.Limit);
            order.Opened.Should().Be(bittrexOrder.TimeStamp);
            order.IsClosed.Should().BeTrue();
            order.Closed.Should().Be(bittrexOrder.Closed.GetValueOrDefault());
        }
    }
}
