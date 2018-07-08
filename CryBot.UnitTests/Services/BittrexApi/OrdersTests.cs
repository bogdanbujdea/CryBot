using Bittrex.Net.Objects;
using Bittrex.Net.Interfaces;

using CryBot.Contracts;

using CryptoExchange.Net;

using FluentAssertions;

using Moq;

using System.Threading.Tasks;
using System.Collections.Generic;

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

        [Fact]
        public async Task SuccessfulOrderCall_ShouldReturn_CryptoOrders()
        {
            _bittrexClientMock.Setup(b => b.GetOrderHistoryAsync(It.IsAny<string>()))
                .ReturnsAsync(() => CreateCompletedOrdersResponse(new List<BittrexOrderHistoryOrder>(), null));
            var ordersResponse = await _bittrexApi.GetCompletedOrdersAsync();
            ordersResponse.IsSuccessful.Should().BeTrue();
        }

        [Fact]
        public async Task FailedHistoryCall_ShouldReturn_ErrorResponse()
        {
            
            _bittrexClientMock.Setup(b => b.GetOrderHistoryAsync(It.IsAny<string>()))
                .ReturnsAsync(() => CreateCompletedOrdersResponse(null, new ServerError("test")));
            var ordersResponse = await _bittrexApi.GetCompletedOrdersAsync();
            ordersResponse.IsSuccessful.Should().BeFalse();
            ordersResponse.ErrorMessage.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task SuccessfulHistoryCall_ShouldReturn_ConvertedOrders()
        {
            _bittrexClientMock.Setup(b => b.GetOrderHistoryAsync(It.IsAny<string>()))
                .ReturnsAsync(() => CreateCompletedOrdersResponse(new List<BittrexOrderHistoryOrder>
                {
                    new BittrexOrderHistoryOrder
                    {
                        Exchange = "BTC-XLM"
                    }
                }, null));
            var ordersResponse = await _bittrexApi.GetCompletedOrdersAsync();
            ordersResponse.Content.Count.Should().Be(1);
            ordersResponse.Content[0].Market.Should().Be("BTC-XLM");
        }

        [Fact]
        public async Task BuyCoin_Should_HaveValidOrder()
        {
            await _bittrexApi.BuyCoinAsync(new CryptoOrder());
        }

        private static CallResult<BittrexOrderHistoryOrder[]> CreateCompletedOrdersResponse(
            List<BittrexOrderHistoryOrder> bittrexOrders,
            Error serverError)
        {
            return new CallResult<BittrexOrderHistoryOrder[]>(bittrexOrders?.ToArray(), serverError);
        }
        
        private static CallResult<BittrexOpenOrdersOrder[]> CreateOpenOrdersResponse(
            List<BittrexOpenOrdersOrder> bittrexOpenOrders,
            Error serverError)
        {
            return new CallResult<BittrexOpenOrdersOrder[]>(bittrexOpenOrders?.ToArray(), serverError);
        }
    }
}
