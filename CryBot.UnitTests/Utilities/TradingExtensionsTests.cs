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
            var bittrexBalance = new BittrexBalance();
            bittrexBalance.Currency = "XLM";
            bittrexBalance.Available = 100;
            bittrexBalance.Balance = 100;
            bittrexBalance.Pending = 0;
            var coinBalance = bittrexBalance.ConvertToCoinBalance();
            coinBalance.Currency.Should().Be("XLM");
            coinBalance.MarketName.Should().Be("BTC-XLM");
            coinBalance.Balance.Should().Be(100);
        }

        
        [Fact]
        public void ToCryptoORder_Should_Convert()
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
            } ;
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
            order.Closed.Should().Be(bittrexOrder.Closed.GetValueOrDefault());
        }}
}
