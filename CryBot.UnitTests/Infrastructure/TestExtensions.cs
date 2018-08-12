using CryBot.Core.Trader;
using CryBot.Core.Exchange;
using CryBot.Core.Strategies;
using CryBot.Core.Exchange.Models;

using Moq;

namespace CryBot.UnitTests.Infrastructure
{
    public static class TestExtensions
    {
        public static void SetTradeAction(this Mock<ITradingStrategy> strategy, TradeAction tradeAction)
        {
            strategy.Setup(s => s.CalculateTradeAction(It.IsAny<Ticker>(), It.IsAny<Trade>())).Returns(tradeAction);
        }
        
        public static void MockBuyingTrade(this Mock<ICryptoApi> cryptoApiMock, CryptoOrder order)
        {
            cryptoApiMock.Setup(s => s.BuyCoinAsync(It.IsAny<CryptoOrder>())).ReturnsAsync(new CryptoResponse<CryptoOrder>(order));
        }
   
        public static void MockSellingTrade(this Mock<ICryptoApi> cryptoApiMock, CryptoOrder order)
        {
            cryptoApiMock.Setup(s => s.SellCoinAsync(It.IsAny<CryptoOrder>())).ReturnsAsync(new CryptoResponse<CryptoOrder>(order));
        }
   
        public static void MockCancelTrade(this Mock<ICryptoApi> cryptoApiMock, CryptoOrder cryptoOrder)
        {
            cryptoApiMock.Setup(s => s.CancelOrder(It.IsAny<string>())).ReturnsAsync(new CryptoResponse<CryptoOrder>(cryptoOrder));
        }
    }
}
