using Bittrex.Net.Objects;
using CryBot.Core.Utilities;
using FluentAssertions;
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
    }
}
