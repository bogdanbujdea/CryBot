using Bitmex.NET;
using Bitmex.NET.Dtos;
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

        public async Task<string> GoLong(MarketInfo marketInfo)
        {
            await PrepareForMarketShift(marketInfo);

            var buyPrice = await CalculateFirstOrderPrice(marketInfo, 0.995M);

            Logger.Log($"Buying at {buyPrice}");
            var orderDto = await ExecuteOrder(marketInfo.Market, marketInfo.Quantity, buyPrice, OrderSide.Buy);
            var takeProfitPrice = RoundPrice(orderDto.Price.GetValueOrDefault(), 2, marketInfo.Round);
            Logger.Log($"Take profit at {takeProfitPrice}");
            
            var stopLossPrice = RoundPrice(buyPrice, marketInfo.StopLossPercentage, marketInfo.Round);
            var triggerPrice = AddUnit(stopLossPrice, 2, marketInfo.Round);
            await ExecuteStopLossOrder(marketInfo.Market, marketInfo.Quantity, stopLossPrice, triggerPrice, OrderSide.Sell);
            await ExecuteTakeProfitOrder(marketInfo.Market, marketInfo.Quantity, takeProfitPrice, AddUnit(takeProfitPrice, 2, marketInfo.Round), OrderSide.Sell);
            return $"Bought {orderDto.OrderQty} {marketInfo.Market} at {orderDto.Price}, take profit set to {takeProfitPrice}";
        }

        private decimal AddUnit(decimal price, int units, int round)
        {
            if (round == 0)
            {
                return price + units;
            }
            var add = 1M / (decimal) (Math.Pow(10, round));
            return price + (add * units);
        }

        public async Task<string> GoShort(MarketInfo marketInfo)
        {
            await PrepareForMarketShift(marketInfo);

            var sellPrice = await CalculateFirstOrderPrice(marketInfo, 1.005M);

            Logger.Log($"Selling at {sellPrice}");
            var orderDto = await ExecuteOrder(marketInfo.Market, marketInfo.Quantity, sellPrice, OrderSide.Sell);
            var takeProfitPrice = RoundPrice(orderDto.Price.GetValueOrDefault(), -2, marketInfo.Round);
            Logger.Log($"Take profit at {takeProfitPrice}");
            var stopLossPrice = RoundPrice(sellPrice, marketInfo.StopLossPercentage, marketInfo.Round);
            var triggerPrice = AddUnit(stopLossPrice, -2, marketInfo.Round);
            await ExecuteStopLossOrder(marketInfo.Market, marketInfo.Quantity, stopLossPrice, triggerPrice, OrderSide.Buy);
            await ExecuteTakeProfitOrder(marketInfo.Market, marketInfo.Quantity, takeProfitPrice, AddUnit(takeProfitPrice, -2, marketInfo.Round), OrderSide.Buy);
            return $"Sold {orderDto.OrderQty} {marketInfo.Market} at {orderDto.Price}, stop order set to {takeProfitPrice}";
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

        private async Task<OrderDto> ExecuteOrder(string market, int quantity, decimal price, OrderSide orderSide)
        {
            return await _bitmexApiService.Execute(BitmexApiUrls.Order.PostOrder,
                OrderPOSTRequestParams.CreateSimpleLimit(market, quantity, price, orderSide));
        }
        
        private async Task<OrderDto> ExecuteStopLossOrder(string market, int quantity, decimal sellPrice, decimal trigger, OrderSide orderSide)
        {
            var apiActionAttributes = new ApiActionAttributes<OrderPOSTRequestParams, OrderDto>("order", HttpMethods.POST);
            return await _bitmexApiService.Execute(apiActionAttributes, new OrderPOSTRequestParams
            {
                Symbol = market,
                Side = Enum.GetName(typeof(OrderSide), orderSide),
                OrderQty = quantity,
                OrdType = "StopLimit",
                StopPx = trigger,
                Price = sellPrice,
                ExecInst = "Close,LastPrice",
            });
            /*return await _bitmexApiService.Execute(BitmexApiUrls.Order.PostOrder,
                OrderPOSTRequestParams.CreateLimitStopOrder(market, quantity, trigger, sellPrice, orderSide));*/
        }

        private async Task<OrderDto> ExecuteTakeProfitOrder(string market, int quantity, decimal price, decimal trigger, OrderSide orderSide)
        {
            var apiActionAttributes = new ApiActionAttributes<OrderPOSTRequestParams, OrderDto>("order", HttpMethods.POST);
            return await _bitmexApiService.Execute(apiActionAttributes, new OrderPOSTRequestParams
            {
                Symbol = market,
                Side = Enum.GetName(typeof(OrderSide), orderSide),
                OrderQty = quantity,
                OrdType = "LimitIfTouched",
                StopPx = trigger,
                Price = price,
                ExecInst = "Close,LastPrice",
                TimeInForce = "GoodTillCancel"
            });
        }
        private decimal RoundPrice(decimal stopPrice, decimal percentage, int round)
        {
            return Math.Round(stopPrice * (1 + percentage/100), round);
        }

        private async Task PrepareForMarketShift(MarketInfo marketInfo)
        {
            await _bitmexApiService.Execute(BitmexApiUrls.Order.DeleteOrderAll, new OrderAllDELETERequestParams
            {
                Symbol=marketInfo.Market
            });
            await _bitmexApiService.Execute(BitmexApiUrls.Order.PostOrder, OrderPOSTRequestParams.ClosePositionByMarket(marketInfo.Market));

            await SetLeverage(marketInfo.Market, marketInfo.Leverage);
        }

        private async Task SetLeverage(string market, int leverage)
        {
            var positionLeveragePostRequestParams = new PositionLeveragePOSTRequestParams();
            positionLeveragePostRequestParams.Leverage = leverage;
            positionLeveragePostRequestParams.Symbol = market;
            await _bitmexApiService.Execute(BitmexApiUrls.Position.PostPositionLeverage, positionLeveragePostRequestParams);
        }

        private async Task<decimal> CalculateFirstOrderPrice(MarketInfo marketInfo, decimal percentage)
        {
            var bitcoinOrderBook = await _bitmexApiService.Execute(BitmexApiUrls.OrderBook.GetOrderBookL2,
                new OrderBookL2GETRequestParams { Depth = 1, Symbol = marketInfo.Market });
            var priceSum = (bitcoinOrderBook[0].Price + bitcoinOrderBook[1].Price);
            return Math.Round((priceSum / 2) * percentage, marketInfo.Round);
        }
    }
}
