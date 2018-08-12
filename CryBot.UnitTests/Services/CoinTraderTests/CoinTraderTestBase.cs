using CryBot.Core.Trader;
using CryBot.Core.Storage;
using CryBot.Core.Strategies;
using CryBot.Core.Notifications;
using CryBot.Core.Exchange.Models;
using CryBot.UnitTests.Infrastructure;

using Moq;

using System.Threading.Tasks;
using System.Reactive.Subjects;
using System.Collections.Generic;

namespace CryBot.UnitTests.Services.CoinTraderTests
{
    public abstract class CoinTraderTestBase : TestBase
    {
        protected readonly CoinTrader CoinTrader;

        protected string Market { get; }

        protected CoinTraderTestBase()
        {
            Market = "BTC-ETC";
            var pushManagerMock = new Mock<IPushManager>();
            CoinTrader = new CoinTrader(CryptoApiMock.Object, OrleansClientMock.Object, HubNotifierMock.Object, pushManagerMock.Object)
            {
                Strategy = Strategy.Object
            };
            var tickerSubject = new Subject<Ticker>();
            var orderSubject = new Subject<CryptoOrder>();
            CryptoApiMock.SetupGet(c => c.TickerUpdated).Returns(tickerSubject);
            CryptoApiMock.SetupGet(c => c.OrderUpdated).Returns(orderSubject);
            TraderGrainMock.Setup(t => t.IsInitialized()).ReturnsAsync(true);
            HubNotifierMock.Setup(h => h.UpdateTrader(It.IsAny<TraderState>())).Returns(Task.CompletedTask);
            HubNotifierMock.Setup(h => h.UpdateTicker(It.IsAny<Ticker>())).Returns(Task.CompletedTask);
            TraderGrainMock.Setup(t => t.UpdateTrades(It.IsAny<List<Trade>>())).Returns(Task.CompletedTask);
            pushManagerMock.Setup(p => p.TriggerPush(It.IsAny<PushMessage>())).Returns(Task.CompletedTask);
            TraderGrainMock.Setup(c => c.GetTraderData()).ReturnsAsync(new TraderState { Trades = new List<Trade>() });
            OrleansClientMock.Setup(c => c.GetGrain<ITraderGrain>(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(TraderGrainMock.Object);
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
