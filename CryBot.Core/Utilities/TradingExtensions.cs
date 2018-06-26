using Bittrex.Net.Objects;

using CryBot.Core.Models;

namespace CryBot.Core.Utilities
{
    public static class TradingExtensions
    {

        public static CoinBalance ConvertToCoinBalance(this BittrexBalance bittrexBalance)
        {
            return new CoinBalance
            {
                Currency = bittrexBalance.Currency,
                MarketName = bittrexBalance.Currency.ToMarket(),
                Balance = bittrexBalance.Balance.GetValueOrDefault(),
            };
        }

        public static string ToMarket(this string currency)
        {
            return $"BTC-{currency}";
        }
    }
}
