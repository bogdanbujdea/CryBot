using CryBot.Core.Models;
using CryBot.Core.Services;
using CryBot.UnitTests.Infrastructure;

using System.Threading.Tasks;
using System.Reactive.Subjects;

namespace CryBot.UnitTests.Services.CoinTraderTests
{
    public abstract class CoinTraderTestBase : TestBase
    {
        protected readonly CoinTrader CoinTrader;

        protected string Market { get; set; }

        protected CoinTraderTestBase()
        {
            CoinTrader = new CoinTrader(CryptoApiMock.Object) { Strategy = Strategy.Object };
            Market = "BTC-ETC";
            var tickerSubject = new Subject<Ticker>();
            var orderSubject = new Subject<CryptoOrder>();
            CryptoApiMock.SetupGet(c => c.TickerUpdated).Returns(tickerSubject);
            CryptoApiMock.SetupGet(c => c.OrderUpdated).Returns(orderSubject);
        }

        protected async Task InitializeTrader(TradeAction tradeAction)
        {
            CryptoApiMock.MockSellingTrade(new CryptoOrder());
            Strategy.SetupGet(strategy => strategy.Settings).Returns(new TraderSettings { TradingBudget = 1000 });
            Strategy.SetTradeAction(tradeAction);
            CoinTrader.Initialize(Market);
            CoinTrader.Strategy = Strategy.Object;
            await CoinTrader.StartAsync();
        }
    }
}
