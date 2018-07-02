using Bittrex.Net.Interfaces;

using CryBot.Core.Models;
using CryBot.Core.Utilities;

using System;
using System.Threading.Tasks;

namespace CryBot.Core.Services
{
    public class FakeBittrexApi : BittrexApi
    {
        public static int OrderCount = 0;

        public FakeBittrexApi(IBittrexClient bittrexClient) : base(bittrexClient)
        {
        }

        public override Task<CryptoResponse<CryptoOrder>> BuyCoinAsync(CryptoOrder cryptoOrder)
        {
            cryptoOrder.Uuid = "BO-" + OrderCount++.ToString();
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(5000);
                    var ticker = await GetTickerAsync(cryptoOrder.Market);
                    if (ticker.Content.Ask <= cryptoOrder.PricePerUnit)
                    {
                        Console.WriteLine($"Closed order {cryptoOrder.Uuid}");
                        cryptoOrder.IsClosed = true;
                        OnOrderUpdate(cryptoOrder);
                        return;
                    }
                }
            });
            return Task.FromResult(new CryptoResponse<CryptoOrder>(cryptoOrder)); ;
        }

        public override Task<CryptoResponse<CryptoOrder>> SellCoinAsync(CryptoOrder sellOrder)
        {
            sellOrder.Uuid = "SO-" + OrderCount++;
            Task.Run(async () =>
            {
                await Task.Delay(2000);
                sellOrder.IsClosed = true;
                OnOrderUpdate(sellOrder);
            });
            return Task.FromResult(new CryptoResponse<CryptoOrder>(sellOrder)); ;
        }
    }
}
