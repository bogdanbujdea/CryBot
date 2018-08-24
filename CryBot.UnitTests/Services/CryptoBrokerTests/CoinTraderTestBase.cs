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

namespace CryBot.UnitTests.Services.CryptoBrokerTests
{
    public abstract class CoinTraderTestBase : TestBase
    {
        protected CryptoBroker CryptoBroker;

        protected string Market { get; set; }

        protected CoinTraderTestBase()
        {
            Reset();
        }

        protected void Reset()
        {
            Market = "BTC-ETC";
            var pushManagerMock = new Mock<IPushManager>();
            Strategy.SetupGet(strategy => strategy.Settings).Returns(new TraderSettings {TradingBudget = 1000});
            CryptoBroker = new CryptoBroker(CryptoApiMock.Object)
            {
                Strategy = Strategy.Object,
                IsInTestMode = true
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
            var traderState = new TraderState
            {
                Trades = new List<Trade>(),
                Budget = new Budget(),
                Settings = new TraderSettings {TradingBudget = 1000}
            };
            CryptoBroker.TraderState = traderState;
            TraderGrainMock.Setup(c => c.GetTraderData()).ReturnsAsync(traderState);
            OrleansClientMock.Setup(c => c.GetGrain<ITraderGrain>(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(TraderGrainMock.Object);
        }

        protected void InitializeTrader(TradeAction tradeAction)
        {
            CryptoApiMock.MockSellingTrade(new CryptoOrder());
            Strategy.SetupGet(strategy => strategy.Settings).Returns(new TraderSettings { TradingBudget = 1000 });
            Strategy.SetTradeAction(tradeAction);
            CryptoBroker.Initialize(new TraderState
            {
                Trades = new List<Trade>(),
                Market = Market,
                Settings = new TraderSettings { TradingBudget = 1000 }
            });
            CryptoBroker.Strategy = Strategy.Object;
        }
    }
}
