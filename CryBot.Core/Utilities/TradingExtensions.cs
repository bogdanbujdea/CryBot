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
                Uuid = openOrder.Uuid.GetValueOrDefault().ToString(),
                Opened = openOrder.Opened,
                Closed = openOrder.Closed.GetValueOrDefault(),
                Limit = openOrder.Limit.RoundSatoshi(),
                QuantityRemaining = openOrder.QuantityRemaining.RoundSatoshi()
            };
        }

        public static CryptoOrder ToCryptoOrder(this BittrexStreamOrderData closedOrderData)
        {
            var closedOrder = closedOrderData.Order;
            return new CryptoOrder
            {
                Market = closedOrder.Market,
                OrderType = closedOrder.OrderType == OrderSideExtended.LimitBuy ? CryptoOrderType.LimitBuy : CryptoOrderType.LimitSell,
                Price = closedOrder.Price.RoundSatoshi(),
                Quantity = closedOrder.Quantity.RoundSatoshi(),
                PricePerUnit = closedOrder.Limit.RoundSatoshi(),
                CommissionPaid = closedOrder.CommissionPaid.RoundSatoshi(),
                Canceled = closedOrder.CancelInitiated,
                Uuid = closedOrder.Uuid.ToString(),
                Opened = closedOrder.Opened,
                Closed = closedOrder.Closed.GetValueOrDefault(),
                Limit = closedOrder.Limit.RoundSatoshi(),
                QuantityRemaining = closedOrder.QuantityRemaining.RoundSatoshi()
            };
        }

        public static CryptoOrder ToCryptoOrder(this BittrexOrderHistoryOrder completedOrder)
        {
            return new CryptoOrder
            {
                Market = completedOrder.Exchange,
                OrderType = completedOrder.OrderType == OrderSideExtended.LimitBuy ? CryptoOrderType.LimitBuy : CryptoOrderType.LimitSell,
                Price = completedOrder.Price.RoundSatoshi(),
                Quantity = completedOrder.Quantity.RoundSatoshi(),
                PricePerUnit = completedOrder.Limit.RoundSatoshi(),
                CommissionPaid = completedOrder.Commission.RoundSatoshi(),
                Uuid = completedOrder.OrderUuid.ToString(),
                Opened = completedOrder.TimeStamp,
                Closed = completedOrder.Closed.GetValueOrDefault(),
                Limit = completedOrder.Limit.RoundSatoshi(),
                QuantityRemaining = completedOrder.QuantityRemaining.RoundSatoshi(),
                IsClosed = true
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

        public static bool ReachedHighStopLoss(this decimal currentPrice, decimal maxPrice, decimal minimumTakeProfit, decimal percentage, decimal buyPrice)
        {
            if (currentPrice < minimumTakeProfit)
                return false;
            var difference = maxPrice - buyPrice;
            if (difference * percentage > (minimumTakeProfit - buyPrice) && difference * percentage >= (currentPrice - buyPrice))
                return true;
            return false;
        }

        public static bool ReachedStopLoss(this decimal currentPrice, decimal buyPrice, decimal stopLoss)
        {
            return buyPrice * stopLoss.ToPercentageMultiplier() >= currentPrice;
        }

        public static decimal ToPercentageMultiplier(this decimal percentage)
        {
            return 1 + (percentage / 100);
        }
    }
}
