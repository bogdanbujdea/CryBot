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
                if (currentTrade.BuyOrder.Uuid == null)
                {
                    tradeAction.OrderPricePerUnit = ticker.Bid;
                    tradeAction.Reason = TradeReason.FirstTrade;
                    tradeAction.TradeAdvice = TradeAdvice.Buy;
                    return tradeAction;
                }
                if (currentTrade.BuyOrder.IsOpened && currentTrade.BuyOrder.Opened.Expired(TimeSpan.FromHours(5), ticker.Timestamp))
                {
                    tradeAction.OrderPricePerUnit = ticker.Bid;
                    tradeAction.Reason = TradeReason.ExpiredBuyOrder;
                    tradeAction.TradeAdvice = TradeAdvice.Cancel;
                    return tradeAction;
                }
                if (currentTrade.Profit > Settings.MinimumTakeProfit && 
                    ticker.Bid.ReachedHighStopLoss(currentTrade.MaxPricePerUnit,
                        currentTrade.BuyOrder.PricePerUnit * Settings.MinimumTakeProfit.ToPercentageMultiplier() * Consts.BittrexCommission,
                        Settings.HighStopLossPercentage.ToPercentageMultiplier(), currentTrade.BuyOrder.PricePerUnit))
                {
                    tradeAction.OrderPricePerUnit = ticker.Bid;
                    tradeAction.Reason = TradeReason.TakeProfit;
                    tradeAction.TradeAdvice = TradeAdvice.Sell;
                    return tradeAction;
                }
            
                if(Settings.BuyTrigger < Settings.StopLoss)
                {
                    tradeAction.TradeAdvice = TradeAdvice.Hold;
                    return tradeAction;
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
                    tradeAction.Reason = TradeReason.StopLoss;
                    tradeAction.TradeAdvice = TradeAdvice.Sell;
                    tradeAction.OrderPricePerUnit = ticker.Bid;
                    return tradeAction;
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
            currentTrade.Profit = profit;
        }
    }
}