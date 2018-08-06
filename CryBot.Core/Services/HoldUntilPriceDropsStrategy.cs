using CryBot.Core.Models;
using CryBot.Core.Utilities;

using System;

namespace CryBot.Core.Services
{
    public class HoldUntilPriceDropsStrategy : ITradingStrategy
    {
        private static volatile object _syncRoot = new object();
        public HoldUntilPriceDropsStrategy()
        {
            Settings = TraderSettings.Default;
        }

        public string Name { get; } = "HoldUntilPriceDrops";

        public TraderSettings Settings { get; set; }

        public TradeAction CalculateTradeAction(Ticker ticker, Trade currentTrade)
        {
            lock (_syncRoot)
            {
                var tradeAction = new TradeAction();

                UpdateTrade(ticker, currentTrade);

                if (currentTrade.Status == TradeStatus.Empty)
                {
                    tradeAction.OrderPricePerUnit = ticker.Bid;
                    tradeAction.Reason = TradeReason.FirstTrade;
                    tradeAction.TradeAdvice = TradeAdvice.Buy;
                    return tradeAction;
                }

                if (currentTrade.BuyOrder.IsOpened && currentTrade.BuyOrder.Opened.Expired(TimeSpan.FromMinutes(5), ticker.Timestamp))
                {
                    return TradeAction.Create(TradeAdvice.Cancel, TradeReason.ExpiredBuyOrder, ticker.Bid);
                }

                if (currentTrade.Status == TradeStatus.Buying || currentTrade.Status == TradeStatus.Selling)
                {
                    return TradeAction.Create(TradeAdvice.Hold);
                }

                if (currentTrade.Profit > Settings.MinimumTakeProfit &&
                    ticker.Bid.ReachedHighStopLoss(currentTrade.MaxPricePerUnit,
                        currentTrade.BuyOrder.PricePerUnit * Settings.MinimumTakeProfit.ToPercentageMultiplier() * Consts.BittrexCommission,
                        Settings.HighStopLossPercentage.ToPercentageMultiplier(), currentTrade.BuyOrder.PricePerUnit))
                {
                    return TradeAction.Create(TradeAdvice.Sell, TradeReason.TakeProfit, ticker.Bid);
                }

                if (currentTrade.TriggeredBuy == false && ticker.Bid.ReachedBuyPrice(currentTrade.BuyOrder.PricePerUnit, Settings.BuyTrigger))
                {
                    tradeAction.Reason = TradeReason.BuyTrigger;
                    tradeAction.TradeAdvice = TradeAdvice.Buy;
                    tradeAction.OrderPricePerUnit = ticker.Bid;
                    currentTrade.TriggeredBuy = true;
                    return tradeAction;
                }

                if (ticker.Bid.ReachedStopLoss(currentTrade.BuyOrder.PricePerUnit, Settings.StopLoss))
                {
                    return TradeAction.Create(TradeAdvice.Sell, TradeReason.StopLoss, ticker.Bid);
                }
                return tradeAction;
            }
        }

        private static void UpdateTrade(Ticker ticker, Trade currentTrade)
        {
            currentTrade.CurrentTicker = ticker;
            if (currentTrade.MaxPricePerUnit < ticker.Bid)
                currentTrade.MaxPricePerUnit = ticker.Bid;
            var profit = currentTrade.BuyOrder.PricePerUnit.GetReadablePercentageChange(ticker.Bid, true);
            if (currentTrade.Status == TradeStatus.Bought || currentTrade.Status == TradeStatus.Selling)
                currentTrade.Profit = profit;
        }
    }
}