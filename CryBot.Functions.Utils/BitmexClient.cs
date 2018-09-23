using Bitmex.NET;
using Bitmex.NET.Models;

using System;
using System.Threading.Tasks;

namespace CryBot.Functions.Utils
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
            var bitmexAuthorization = new BitmexAuthorization
            {
                BitmexEnvironment = Environment.GetEnvironmentVariable("bitmexEnvironment") == "Test"
                    ? BitmexEnvironment.Test
                    : BitmexEnvironment.Prod,
                Key = Environment.GetEnvironmentVariable("bitmexTestnetKey"),
                Secret = Environment.GetEnvironmentVariable("bitmexTestnetSecret")
            };
            var bitmexApiService = BitmexApiService.CreateDefaultApi(bitmexAuthorization);
            return bitmexApiService;
        }

        public async Task<string> GoLong(MarketInfo marketInfo)
        {
            await _bitmexApiService.Execute(BitmexApiUrls.Order.PostOrder, OrderPOSTRequestParams.ClosePositionByMarket(marketInfo.Market));
            await SetLeverage(marketInfo.Market, marketInfo.Leverage);
            var orderDto = await _bitmexApiService.Execute(BitmexApiUrls.Order.PostOrder,
                OrderPOSTRequestParams.CreateSimpleMarket(marketInfo.Market, marketInfo.Quantity, OrderSide.Buy));
            var stopPrice = Math.Round((decimal)orderDto.Price * 1.02M, 0);
            //await _bitmexApiService.Execute(BitmexApiUrls.Order.PostOrder, OrderPOSTRequestParams.CreateSimpleHidenLimit(marketInfo.Market, marketInfo.Quantity, buyPrice - 50, OrderSide.Buy));
            await _bitmexApiService.Execute(BitmexApiUrls.Order.PostOrder, OrderPOSTRequestParams.CreateSimpleLimit(marketInfo.Market, marketInfo.Quantity, stopPrice, OrderSide.Sell));
            return $"Bought {orderDto.OrderQty} {marketInfo.Market} at {orderDto.Price}, stop order set to {stopPrice}";
        }

        public async Task<string> GoShort(MarketInfo marketInfo)
        {
            await _bitmexApiService.Execute(BitmexApiUrls.Order.PostOrder, OrderPOSTRequestParams.ClosePositionByMarket(marketInfo.Market));

            await SetLeverage(marketInfo.Market, marketInfo.Leverage);
            //create limit order
            var orderDto = await _bitmexApiService.Execute(BitmexApiUrls.Order.PostOrder,
                OrderPOSTRequestParams.CreateSimpleMarket(marketInfo.Market, marketInfo.Quantity, OrderSide.Sell));
            var stopPrice = Math.Round(orderDto.Price.GetValueOrDefault() * 0.98M, marketInfo.Round);
            //create stop order
            //await _bitmexApiService.Execute(BitmexApiUrls.Order.PostOrder, OrderPOSTRequestParams.CreateSimpleHidenLimit(marketInfo.Market, marketInfo.Quantity, sellPrice + 50, OrderSide.Sell));
            await _bitmexApiService.Execute(BitmexApiUrls.Order.PostOrder, OrderPOSTRequestParams.CreateSimpleLimit(marketInfo.Market, marketInfo.Quantity, stopPrice, OrderSide.Buy));
            //create 
            return $"Sold {orderDto.OrderQty} {marketInfo.Market} at {orderDto.Price}, stop order set to {stopPrice}";
        }

        private async Task SetLeverage(string market, int leverage)
        {
            var positionLeveragePostRequestParams = new PositionLeveragePOSTRequestParams();
            positionLeveragePostRequestParams.Leverage = leverage;
            positionLeveragePostRequestParams.Symbol = market;
            await _bitmexApiService.Execute(BitmexApiUrls.Position.PostPositionLeverage, positionLeveragePostRequestParams);
        }

        private async Task<decimal> GetCurrentPrice(MarketInfo marketInfo)
        {
            var bitcoinOrderBook = await _bitmexApiService.Execute(BitmexApiUrls.OrderBook.GetOrderBookL2,
                new OrderBookL2GETRequestParams { Depth = 1, Symbol = marketInfo.Market });
            var priceSum = (bitcoinOrderBook[0].Price + bitcoinOrderBook[1].Price);
            return Math.Round(priceSum / 2, marketInfo.Round);
        }
    }
}
