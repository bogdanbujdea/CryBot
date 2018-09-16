using CryBot.Core.Trader.Backtesting;

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Bitmex.NET;
using Bitmex.NET.Models;

namespace CryBot.Backtesting
{
    class Program
    {
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
                var bitmexApiService = BitmexApiService.CreateDefaultApi(bitmexAuthorization);

                var bitcoinOrderBook = await bitmexApiService.Execute(BitmexApiUrls.OrderBook.GetOrderBookL2,
                    new OrderBookL2GETRequestParams {Depth = 1, Symbol = market});
                var buyPrice = Math.Round((bitcoinOrderBook[0].Price + bitcoinOrderBook[1].Price) / 2);

                var positions = await bitmexApiService.Execute(BitmexApiUrls.Position.GetPosition, new PositionGETRequestParams { Count = 50 });


                var positionLeveragePostRequestParams = new PositionLeveragePOSTRequestParams();
                positionLeveragePostRequestParams.Leverage = 50;
                positionLeveragePostRequestParams.Symbol = market;
                /*var positionDto = await bitmexApiService.Execute(BitmexApiUrls.Position.PostPositionLeverage,
                    positionLeveragePostRequestParams);*/
                foreach (var position in positions)
                {
                    await bitmexApiService.Execute(BitmexApiUrls.Order.PostOrder, OrderPOSTRequestParams.ClosePositionByMarket(market));
                }
                var positionDto = await bitmexApiService.Execute(BitmexApiUrls.Position.PostPositionLeverage, positionLeveragePostRequestParams);
                var orderDto = await bitmexApiService.Execute(BitmexApiUrls.Order.PostOrder, OrderPOSTRequestParams.CreateSimpleMarket(market, 1500, OrderSide.Sell));
                var round = Math.Round((decimal)orderDto.Price * 0.9M, 0);
                var stopOrder = await bitmexApiService.Execute(BitmexApiUrls.Order.PostOrder, OrderPOSTRequestParams.CreateSimpleHidenLimit(market, 1500, round, OrderSide.Buy));
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
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
