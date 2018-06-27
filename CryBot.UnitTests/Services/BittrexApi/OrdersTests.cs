using Bittrex.Net.Objects;
using Bittrex.Net.Interfaces;

using CryptoExchange.Net;

using FluentAssertions;

using Moq;

using System.Threading.Tasks;
using System.Collections.Generic;
using CryBot.Core.Models;
using Xunit;

namespace CryBot.UnitTests.Services.BittrexApi
{
    public class OrdersTests
    {
        private readonly Mock<IBittrexClient> _bittrexClientMock;
        private readonly Core.Services.BittrexApi _bittrexApi;

        public OrdersTests()
        {
            _bittrexClientMock = new Mock<IBittrexClient>();
            _bittrexApi = new Core.Services.BittrexApi(_bittrexClientMock.Object);
            _bittrexClientMock.Setup(b => b.GetOpenOrdersAsync(It.IsAny<string>())).ReturnsAsync(()
                => CreateOpenOrdersResponse(new List<BittrexOpenOrdersOrder> { new BittrexOpenOrdersOrder() }, null));
        }

        [Fact]
        public async Task GetOpenOrders_Should_ReturnListOfOrders()
        {
            var ordersResponse = await _bittrexApi.GetOpenOrdersAsync();
            ordersResponse.Content.Should().NotBeNull();
        }

        [Fact]
        public async Task SuccessfulCall_Should_ReturnListOfOrders()
        {
            var ordersResponse = await _bittrexApi.GetOpenOrdersAsync();
            ordersResponse.Content.Count.Should().Be(1);
        }

        [Fact]
        public async Task FailedCallForOrders_Should_ReturnFailedResponse()
        {
            _bittrexClientMock.Setup(b => b.GetOpenOrdersAsync(It.IsAny<string>()))
                .ReturnsAsync(CreateOpenOrdersResponse(new List<BittrexOpenOrdersOrder>(), new ServerError("test")));
            var ordersResponse = await _bittrexApi.GetOpenOrdersAsync();
            ordersResponse.ErrorMessage.Should().NotBeNullOrWhiteSpace();
            ordersResponse.IsSuccessful.Should().BeFalse();
        }
        
        private static CallResult<BittrexOpenOrdersOrder[]> CreateOpenOrdersResponse(List<BittrexOpenOrdersOrder> bittrexOpenOrdersOrders, ServerError serverError)
        {
            return new CallResult<BittrexOpenOrdersOrder[]>(bittrexOpenOrdersOrders.ToArray(), serverError);
        }
    }
}
