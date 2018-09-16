using System;
using System.Threading.Tasks;
using Bitmex.NET;
using Bitmex.NET.Models;

namespace DemaSignal
{
    public class BitmexClient
    {
        private readonly IBitmexApiService _bitmexApiService;

        public BitmexClient()
        {
            _bitmexApiService = CreateBitmexClient();
        }

        private IBitmexApiService CreateBitmexClient()
        {
            var bitmexAuthorization = new BitmexAuthorization();
            bitmexAuthorization.BitmexEnvironment = Environment.GetEnvironmentVariable("bitmexEnvironment") == "Test" ? BitmexEnvironment.Test : BitmexEnvironment.Prod;
            bitmexAuthorization.Key = Environment.GetEnvironmentVariable("bitmexTestnetKey");
            bitmexAuthorization.Secret = Environment.GetEnvironmentVariable("bitmexTestnetSecret");
            var bitmexApiService = BitmexApiService.CreateDefaultApi(bitmexAuthorization);
            return bitmexApiService;
        }

        public async Task<string> GoLong(string market)
        {
            await _bitmexApiService.Execute(BitmexApiUrls.Order.PostOrder, OrderPOSTRequestParams.ClosePositionByMarket(market));
            var buyPrice = await GetCurrentPrice(market);
            var orderDto = await _bitmexApiService.Execute(BitmexApiUrls.Order.PostOrder,
                OrderPOSTRequestParams.CreateSimpleLimit(market, 1500, buyPrice, OrderSide.Buy));
            var stopPrice = Math.Round((decimal)orderDto.Price * 1.005M, 0);
            await _bitmexApiService.Execute(BitmexApiUrls.Order.PostOrder, OrderPOSTRequestParams.CreateSimpleHidenLimit(market, 1500, stopPrice, OrderSide.Sell));
            return $"Bought {orderDto.OrderQty} at {orderDto.Price}, stop order set to {stopPrice}";
        }

        public async Task<string> GoShort(string market)
        {
            await _bitmexApiService.Execute(BitmexApiUrls.Order.PostOrder, OrderPOSTRequestParams.ClosePositionByMarket(market));
            var sellPrice = await GetCurrentPrice(market);
            var orderDto = await _bitmexApiService.Execute(BitmexApiUrls.Order.PostOrder,
                OrderPOSTRequestParams.CreateSimpleLimit(market, 1500, sellPrice, OrderSide.Sell));
            var stopPrice = Math.Round((decimal)orderDto.Price * 0.995M, 0);
            await _bitmexApiService.Execute(BitmexApiUrls.Order.PostOrder, OrderPOSTRequestParams.CreateSimpleHidenLimit(market, 1500, stopPrice, OrderSide.Buy));
            return $"Sold {orderDto.OrderQty} at {orderDto.Price}, stop order set to {stopPrice}";
        }

        private async Task<decimal> GetCurrentPrice(string market)
        {
            var bitcoinOrderBook = await _bitmexApiService.Execute(BitmexApiUrls.OrderBook.GetOrderBookL2,
                new OrderBookL2GETRequestParams { Depth = 1, Symbol = market });
            var buyPrice = Math.Round((bitcoinOrderBook[0].Price + bitcoinOrderBook[1].Price) / 2);
            return buyPrice;
        }
    }
}
