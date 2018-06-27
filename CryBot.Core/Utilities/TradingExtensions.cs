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
                Quantity = bittrexBalance.Balance.GetValueOrDefault(),
            };
        }
        
        public static CryptoOrder ToCryptoOrder(this BittrexOpenOrdersOrder openOrder)
        {
            return new CryptoOrder
            {
                Market = openOrder.Exchange,
                OrderType = openOrder.OrderType == OrderSideExtended.LimitBuy ? CryptoOrderType.LimitBuy : CryptoOrderType.LimitSell,
                Price = openOrder.Price,
                Quantity = openOrder.Quantity,
                PricePerUnit = openOrder.Limit,
                CommissionPaid = openOrder.CommissionPaid,
                Canceled = openOrder.CancelInitiated,
                Uuid = openOrder.Uuid.GetValueOrDefault(),
                Opened = openOrder.Opened,
                Closed = openOrder.Closed.GetValueOrDefault(),
                Limit = openOrder.Limit,
                QuantityRemaining = openOrder.QuantityRemaining
            };
        }

        public static string ToMarket(this string currency)
        {
            return $"BTC-{currency}";
        }
    }
}
