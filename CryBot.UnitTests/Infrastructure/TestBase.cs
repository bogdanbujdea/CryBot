using CryBot.Core.Models;
using CryBot.Core.Services;
using Moq;

namespace CryBot.UnitTests.Infrastructure
{
    public class TestBase
    {
        protected Mock<ITradingStrategy> Strategy { get; set; }
        protected Mock<ICryptoApi> CryptoApiMock { get; set; }

        public TestBase()
        {
            Strategy = new Mock<ITradingStrategy>();
            CryptoApiMock = new Mock<ICryptoApi>();
        }
    }
}
