using Bittrex.Net.Objects;

using CryBot.Core.Models;

using System;

namespace CryBot.Core.Utilities
{
    public static class TradingExtensions
    {

        public static CoinBalance ConvertToCoinBalance(this BittrexBalance bittrexBalance)
        {
            return new CoinBalance
            {
                Market = bittrexBalance.Currency.ToMarket(),
                Quantity = bittrexBalance.Balance.GetValueOrDefault().RoundSatoshi(),
                Available = bittrexBalance.Available.GetValueOrDefault().RoundSatoshi()
            };
        }
        
        public static CryptoOrder ToCryptoOrder(this BittrexOpenOrdersOrder openOrder)
        {
            return new CryptoOrder
            {
                Market = openOrder.Exchange,
                OrderType = openOrder.OrderType == OrderSideExtended.LimitBuy ? CryptoOrderType.LimitBuy : CryptoOrderType.LimitSell,
                Price = openOrder.Price.RoundSatoshi(),
                Quantity = openOrder.Quantity.RoundSatoshi(),
                PricePerUnit = openOrder.Limit.RoundSatoshi(),
                CommissionPaid = openOrder.CommissionPaid.RoundSatoshi(),
                Canceled = openOrder.CancelInitiated,
                Uuid = openOrder.Uuid.GetValueOrDefault(),
                Opened = openOrder.Opened,
                Closed = openOrder.Closed.GetValueOrDefault(),
                Limit = openOrder.Limit.RoundSatoshi(),
                QuantityRemaining = openOrder.QuantityRemaining.RoundSatoshi()
            };
        }

        public static string ToMarket(this string currency)
        {
            return $"BTC-{currency}";
        }

        public static string ToCurrency(this string market)
        {
            return market.Split('-')[1];
        }

        public static decimal RoundSatoshi(this decimal price)
        {
            return Math.Round(price, 8);
        }
    }
}
