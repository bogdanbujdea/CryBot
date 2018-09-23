using Bitmex.NET;
using Bitmex.NET.Dtos;
using Bitmex.NET.Models;

using CryBot.Core.Trader.Backtesting;

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Backtesting
{
    class Program
    {
        private static IBitmexApiService _bitmexApiService;

        static async Task Main(string[] args)
        {
            await PlayWithBitmex();
            return;
            var backtester = new BackTester(Resource.BittrexApiKey, Resource.BittrexApiSecret);
            var market = "BTC-ETC";
            var marketResults = await backtester.FindBestSettings(market);
            var bestResult = marketResults[0];
            Console.WriteLine($"{market}\t{bestResult.Budget.Profit}%\t{bestResult.Settings}");
        }

        private static async Task PlayWithBitmex()
        {
            try
            {
                const string market = "XBTUSD";
                var bitmexAuthorization = new BitmexAuthorization();
                bitmexAuthorization.BitmexEnvironment = BitmexEnvironment.Test;
                bitmexAuthorization.Key = "wcZtcAiFMff8kLWaSLl8U877";
                bitmexAuthorization.Secret = "uOtP5-0sEtiis5d1_Qv1-LW8FLsV3qW9Qsmsf_OWBXVzw-c3";
                _bitmexApiService = BitmexApiService.CreateDefaultApi(bitmexAuthorization);

                await _bitmexApiService.Execute(BitmexApiUrls.Order.DeleteOrderAll, new OrderAllDELETERequestParams
                {
                    Symbol="BCHU18"
                });
                var bitcoinOrderBook = await _bitmexApiService.Execute(BitmexApiUrls.OrderBook.GetOrderBookL2,
                    new OrderBookL2GETRequestParams {Depth = 1, Symbol = market});
                var currentPrice = Math.Round((bitcoinOrderBook[0].Price + bitcoinOrderBook[1].Price) / 2);
                var stopSellOrder = await _bitmexApiService.Execute(BitmexApiUrls.Order.PostOrder, OrderPOSTRequestParams.CreateLimitStopOrder(market, 1500, 6500, 6467, OrderSide.Buy));

                var positions = await _bitmexApiService.Execute(BitmexApiUrls.Position.GetPosition, new PositionGETRequestParams { Count = 50 });

                var positionLeveragePostRequestParams = new PositionLeveragePOSTRequestParams();
                positionLeveragePostRequestParams.Leverage = 50;
                positionLeveragePostRequestParams.Symbol = market;
                /*var positionDto = await bitmexApiService.Execute(BitmexApiUrls.Position.PostPositionLeverage,
                    positionLeveragePostRequestParams);*/
                foreach (var position in positions)
                {
                    await _bitmexApiService.Execute(BitmexApiUrls.Order.PostOrder, OrderPOSTRequestParams.ClosePositionByMarket(market));
                }
                var positionDto = await _bitmexApiService.Execute(BitmexApiUrls.Position.PostPositionLeverage, positionLeveragePostRequestParams);
                var initialOrder = await CreateLimitOrder(market, 1500, currentPrice, OrderSide.Sell);
                var round = Math.Round((decimal)initialOrder.Price * 0.9M, 0);
                var stopOrder = await _bitmexApiService.Execute(BitmexApiUrls.Order.PostOrder, OrderPOSTRequestParams.CreateSimpleHidenLimit(market, 1500, round, OrderSide.Buy));
                var stopLoss = await CreateLimitOrder(market, 1500, currentPrice + 40, OrderSide.Sell);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
        }
        private static async Task<OrderDto> ExecuteOrder(string market, int quantity, decimal price, OrderSide orderSide)
        {
            return await _bitmexApiService.Execute(BitmexApiUrls.Order.PostOrder,
                OrderPOSTRequestParams.CreateSimpleLimit(market, quantity, price, orderSide));
        }
        private static async Task<OrderDto> CreateLimitOrder(string market, int quantity, decimal price, OrderSide orderSide)
        {
            return await _bitmexApiService.Execute(BitmexApiUrls.Order.PostOrder, OrderPOSTRequestParams.CreateSimpleLimit(market, quantity, price, orderSide));
        }

        private static async Task FindBestCommonSettings(BackTester backtester)
        {
            var otherMarkets = new List<string>
            {
                "BTC-ETC",
                "BTC-ETH",
                "BTC-ADA",
                "BTC-XLM",
                "BTC-XRP",
                "BTC-XMR",
                "BTC-LTC",
                "BTC-TRX"
            };
            var topSettings = new List<BackTestResult>();
            foreach (var btcMarket in otherMarkets)
            {
                var marketResults = await backtester.FindBestSettings(btcMarket);
                var bestResult = marketResults[0];
                topSettings.Add(bestResult);
                Console.WriteLine($"{btcMarket}\t{bestResult.Budget.Profit}%\t{bestResult.Settings}");
                foreach (var testMarket in otherMarkets.Where(m => m != btcMarket))
                {
                    bestResult = await GetResultForMarket(testMarket, marketResults[0]);
                    Console.WriteLine($"{btcMarket}---{testMarket}\t{bestResult.Budget.Profit}%");
                }
            }
        }

        private static async Task<BackTestResult> GetResultForMarket(string btcMarket, BackTestResult bestEtcBacktest)
        {
            var backtester = new BackTester(Resource.BittrexApiKey, Resource.BittrexApiSecret);
            var bestResult = await backtester.TrySettings(btcMarket, null, bestEtcBacktest.Settings);
            return bestResult;
        }
    }
}
