using CryBot.Core.Models;
using CryBot.UnitTests.Infrastructure;

using FluentAssertions;

using Moq;

using System.Threading.Tasks;

using Xunit;

namespace CryBot.UnitTests.Services.CoinTraderTests
{

    public class BudgetTests : CoinTraderTestBase
    {
        [Fact]
        public async Task EachTrader_ShouldHave_ABudget()
        {
            await TriggerBuy(1, 100, 1000);

            CoinTrader.Strategy.Settings.TradingBudget.Should().Be(1000);
            CoinTrader.Budget.Available.Should().Be(900);
            CoinTrader.Budget.Invested.Should().Be(1000);
            CryptoApiMock.Verify(c => c.BuyCoinAsync(It.Is<CryptoOrder>(o => o.Price == 1000)), Times.Once);
        }

        [Fact]
        public async Task BuyOrder_Should_CalculateQuantityWithoutCommission()
        {
            await TriggerBuy(100, 1000, 1000);
            CheckBuyOrderInvocation(9.975M, 100, 1000);
        }

        [Fact]
        public async Task BuyOrder_Should_WithdrawFromAvailableBudget()
        {
            CoinTrader.Budget.Available = 0;
            await TriggerBuy(100, 500, 1000);
            CoinTrader.Budget.Available.Should().Be(500);
        }

        [Fact]
        public async Task SuccessfulBuyOrder_Should_DecreaseAvailableBudget()
        {
            await TriggerBuy(100, 1000, 1000);
            CoinTrader.Budget.Available.Should().Be(0);
        }

        [Fact]
        public async Task BuyOrder_Should_IncreaseInvestedBTCIfAvailableBudgetIsNotEnough()
        {
            await TriggerBuy(100, 1000, 1000);
            CoinTrader.Budget.Invested.Should().Be(1000);
        }

        [Fact]
        public async Task CanceledBuyOrder_Should_RestoreAvailableBudget()
        {
            await TriggerBuy(100, 1000, 1000);
            CoinTrader.Budget.Available.Should().Be(0);
            Strategy.SetTradeAction(new TradeAction{TradeAdvice = TradeAdvice.Cancel});

            await CoinTrader.UpdatePrice(new Ticker());

            CoinTrader.Budget.Available.Should().Be(1000);
        }

        private void CheckBuyOrderInvocation(decimal quantity, decimal pricePerUnit, decimal price)
        {
            CryptoApiMock.Verify(c => c.BuyCoinAsync(It.Is<CryptoOrder>(o => o.Price == price
                                                                             && o.PricePerUnit == pricePerUnit
                                                                             && o.Quantity == quantity)), Times.Once);
        }

        private async Task TriggerBuy(decimal pricePerUnit, decimal price, decimal budget)
        {
            var cryptoOrder = new CryptoOrder { PricePerUnit = pricePerUnit, Price = price };
            CryptoApiMock.MockBuyingTrade(cryptoOrder);
            CryptoApiMock.MockCancelTrade(cryptoOrder);
            await InitializeTrader(new TradeAction { TradeAdvice = TradeAdvice.Buy, OrderPricePerUnit = pricePerUnit });
            Strategy.SetupGet(strategy => strategy.Settings).Returns(new TraderSettings { TradingBudget = budget });
            await CoinTrader.UpdatePrice(new Ticker());
        }
    }
}
