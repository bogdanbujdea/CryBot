using CryBot.Core.Storage;
using CryBot.Core.Exchange;
using CryBot.Core.Strategies;
using CryBot.Core.Notifications;

using Moq;

using Orleans;

namespace CryBot.UnitTests.Infrastructure
{
    public class TestBase
    {
        protected Mock<ITradingStrategy> Strategy { get; set; }        
        protected Mock<ICryptoApi> CryptoApiMock { get; set; }        
        protected Mock<IClusterClient> OrleansClientMock { get; set; }       
        protected Mock<IHubNotifier> HubNotifierMock { get; set; }
        protected Mock<ITraderGrain> TraderGrainMock { get; set; }
        protected Mock<IPushManager> PushManagerMock { get; set; }

        public TestBase()
        {
            Strategy = new Mock<ITradingStrategy>();
            CryptoApiMock = new Mock<ICryptoApi>();
            OrleansClientMock = new Mock<IClusterClient>();
            HubNotifierMock = new Mock<IHubNotifier>();
            TraderGrainMock = new Mock<ITraderGrain>();
            PushManagerMock = new Mock<IPushManager>();
        }
    }
}
