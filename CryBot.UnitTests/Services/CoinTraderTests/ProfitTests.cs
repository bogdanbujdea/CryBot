using CryBot.Core.Models;

using FluentAssertions;
using Xunit;

namespace CryBot.UnitTests.Services.CoinTraderTests
{
    public class ProfitTests : CoinTraderTestBase
    {
        [Fact]
        public void SoldCoin_Should_InvokeOrderUpdated()
        {
            CoinTrader.UpdateOrder(new CryptoOrder { OrderType = CryptoOrderType.LimitSell, Price = 1100 });
            CoinTrader.Budget.Available.Should().Be(1100);
        }

        [Fact]
        public void SoldCoin_Should_CalculateCorrectProfit()
        {
            var sellOrder = new CryptoOrder { OrderType = CryptoOrderType.LimitSell, Price = 1100, Uuid = "S" };
            var trade = new Trade
            {
                BuyOrder = new CryptoOrder
                {
                    Price = 1000
                },
                SellOrder = sellOrder
            };
            CoinTrader.Trades.Add(trade);
            CoinTrader.Budget.Profit = 5;
            CoinTrader.Budget.Earned = 1;

            CoinTrader.UpdateOrder(sellOrder);

            CoinTrader.Budget.Profit.Should().Be(15);
            CoinTrader.Budget.Earned.Should().Be(101);
            trade.Profit.Should().Be(10);
        }
    }
}
