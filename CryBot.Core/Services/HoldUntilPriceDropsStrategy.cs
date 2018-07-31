using CryBot.Contracts;
using CryBot.Core.Utilities;

namespace CryBot.Core.Services
{
    public class HoldUntilPriceDropsStrategy : ITradingStrategy
    {
        public HoldUntilPriceDropsStrategy()
        {
            Settings = TraderSettings.Default;
        }

        public string Name { get; } = "HoldUntilPriceDrops";

        public TraderSettings Settings { get; set; }

        public Trade CurrentTrade { get; set; }

        public TradeAction CalculateTradeAction(Ticker ticker)
        {
            var tradeAction = new TradeAction();
            CurrentTrade.CurrentTicker = ticker;
            if (CurrentTrade.MaxPricePerUnit < ticker.Bid)
                CurrentTrade.MaxPricePerUnit = ticker.Bid;
            var profit = CurrentTrade.BuyOrder.PricePerUnit.GetReadablePercentageChange(ticker.Bid, true);
            CurrentTrade.Profit = profit;
            if (profit > Settings.MinimumTakeProfit.ToPercentageMultiplier() && 
                ticker.Bid.ReachedHighStopLoss(CurrentTrade.MaxPricePerUnit,
                CurrentTrade.BuyOrder.PricePerUnit * Settings.MinimumTakeProfit.ToPercentageMultiplier() * Consts.BittrexCommission,
                Settings.HighStopLossPercentage.ToPercentageMultiplier(), CurrentTrade.BuyOrder.PricePerUnit))
            {
                tradeAction.OrderPricePerUnit = ticker.Bid;
                tradeAction.TradeAdvice = TradeAdvice.Sell;
                return tradeAction;
            }
            
            if(Settings.BuyTrigger < Settings.StopLoss)
            {
                tradeAction.TradeAdvice = TradeAdvice.Hold;
                return tradeAction;
            }
            if (CurrentTrade.TriggeredBuy == false && ticker.Bid.ReachedBuyPrice(CurrentTrade.BuyOrder.PricePerUnit, Settings.BuyTrigger))
            {
                tradeAction.TradeAdvice = TradeAdvice.Buy;
                tradeAction.OrderPricePerUnit = ticker.Bid;
                CurrentTrade.TriggeredBuy = true;
                return tradeAction;
            }

            if (ticker.Bid.ReachedStopLoss(CurrentTrade.BuyOrder.PricePerUnit, Settings.StopLoss))
            {
                tradeAction.TradeAdvice = TradeAdvice.Sell;
                tradeAction.OrderPricePerUnit = ticker.Bid;
                return tradeAction;
            }
            return tradeAction;
        }
    }
}