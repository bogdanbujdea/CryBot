using CryBot.Core.Strategies;
using CryBot.Core.Exchange.Models;
using CryBot.UnitTests.Infrastructure;

using Moq;

using System;

using System.Threading.Tasks;

using Xunit;

namespace CryBot.UnitTests.Services.CoinTraderTests
{
    public class OrderValidationTests : CoinTraderTestBase
    {
        [Fact]
        public async Task BuyOrder_Should_BeValid()
        {
            CryptoApiMock.MockBuyingTrade(new CryptoOrder
            {
                Market = Market,
                PricePerUnit = 100,
                Price = 1000,
                IsOpened = true,
                OrderType = CryptoOrderType.LimitBuy,
                Quantity = 9.975M,
                Limit = 100
            });
            Strategy.SetupGet(strategy => strategy.Settings).Returns(new TraderSettings { TradingBudget = 1000 });
            await InitializeTrader(new TradeAction { TradeAdvice = TradeAdvice.Buy, OrderPricePerUnit = 100 });

            await CoinTrader.UpdatePrice(new Ticker());

            CryptoApiMock.Verify(c => c.BuyCoinAsync(It.Is<CryptoOrder>(b => b.PricePerUnit == 100
                                                                             && b.Price == 1000
                                                                             && b.Market == Market
                                                                             && b.IsOpened
                                                                             && b.OrderType == CryptoOrderType.LimitBuy
                                                                             && b.Quantity == 9.975M
                                                                             && b.Limit == 100)), Times.Once);
        }

        [Fact]
        public async Task SellOrder_Should_BeValid()
        {
            CryptoApiMock.MockSellingTrade(new CryptoOrder
            {
                Market = Market,
                PricePerUnit = 110,
                Price = 1100,
                OrderType = CryptoOrderType.LimitSell,
                Quantity = 10M,
                Limit = 110
            });
            
            Strategy.SetupGet(strategy => strategy.Settings).Returns(new TraderSettings { TradingBudget = 1000 });
            await InitializeTrader(new TradeAction { TradeAdvice = TradeAdvice.Sell, OrderPricePerUnit = 110 });
            CoinTrader.Trades[0].BuyOrder.Quantity = 10;
            await CoinTrader.UpdatePrice(new Ticker());

            CryptoApiMock.Verify(c => c.SellCoinAsync(It.Is<CryptoOrder>(s => s.PricePerUnit == 110
                                                                             && s.Price == 1100
                                                                             && s.Market == Market
                                                                             && s.OrderType == CryptoOrderType.LimitSell
                                                                             && s.Quantity == 10M
                                                                             && s.Limit == 110)), Times.Once);
        }
    }
}
