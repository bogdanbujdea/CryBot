using CryBot.Core.Models;
using CryBot.Core.Utilities;

using System;
using System.Linq;
using System.Collections.Generic;

namespace CryBot.Core.Services
{
    public class CryptoTraderBacktester
    {
        private int _newTradeCount;
        private decimal _profitBTC;
        private CryptoTraderStats _stats;
        private string _market;
        private List<Trade> _trades;

        public List<Candle> Candles { get; set; }

        public ITradingStrategy Strategy { get; set; }

        public CryptoTraderStats StartFromFile(string market)
        {
            _market = market;
            var prices = Candles.Select(c => CreateTicker(_market, c.Low, c.High, c.Timestamp)).ToList();
            _trades.Add(CreateTrade(_market, prices[0], Strategy.Settings.TradingBudget));
            foreach (var ticker in prices)
            {
                UpdateTicker(ticker);
            }
            GetStats();
            return _stats;
        }

        public void Initialize()
        {
            _stats = new CryptoTraderStats();
            _trades = new List<Trade>();
            _stats.InvestedBTC += Strategy.Settings.TradingBudget;
            _stats.CurrentBTC = _stats.InvestedBTC;
            _profitBTC = 0M;
            _newTradeCount = 1;
        }

        private void LogText(string text)
        {
            Console.WriteLine(text);
        }

        private void GetStats()
        {
            var totalBTC = _trades.Where(t => t.IsActive).Sum(t => t.CurrentValue);
            _stats.Closed = _trades.Count(t => t.IsActive == false);
            _stats.Opened = _trades.Count(t => t.IsActive);
            LogText($"BTC: {_profitBTC.RoundSatoshi()}");
            LogText($"Trade count: {_newTradeCount}");
            LogText($"Diff BTC: {(_profitBTC - _stats.InvestedBTC).RoundSatoshi()}");
            LogText($"Triggered buys: {_trades.Count(t => t.TriggeredBuy)}");
            LogText($"Closed trades count: {_stats.Closed}");
            LogText($"Opened trades count: {_trades.Count(t => t.IsActive)}");
            _stats.Profit = _stats.InvestedBTC.GetReadablePercentageChange(_stats.InvestedBTC + _profitBTC);
        }

        private void UpdateTicker(Ticker ticker)
        {
            Trade newTrade = null;
            newTrade = null;
            foreach (var trade in _trades.Where(t => t.IsActive))
            {
                if (trade.BuyOrder.IsClosed == false)
                {
                    if (trade.BuyOrder.PricePerUnit >= ticker.Ask)
                    {
                        trade.BuyOrder.Uuid = _trades.IndexOf(trade).ToString();
                        LogText($"FILLED BUY\t{trade.BuyOrder.PricePerUnit}\t{ticker.Timestamp:F}\t{trade.BuyOrder.Price} BTC\t\t{trade.BuyOrder.Uuid}");
                        trade.BuyOrder.IsClosed = true;
                        continue;
                    }

                }

                var tradeAction = Strategy.CalculateTradeAction(ticker, trade);
                if (tradeAction.TradeAdvice == TradeAdvice.Cancel)
                {
                    trade.IsActive = false;
                    trade.Profit = 0;
                    trade.CurrentTicker = new Ticker();
                    newTrade = CreateLowerTrade(ticker);
                    _stats.AvailableBTC += trade.BuyOrder.Price;
                }
                else if (tradeAction.TradeAdvice == TradeAdvice.Buy)
                {
                    _newTradeCount++;
                    ticker.Bid = tradeAction.OrderPricePerUnit;
                    if (_stats.AvailableBTC <= Strategy.Settings.TradingBudget)
                    {
                        _stats.InvestedBTC += Strategy.Settings.TradingBudget;
                        _stats.AvailableBTC = Strategy.Settings.TradingBudget;
                    }

                    LogText($"BUYING due to {tradeAction.Reason} at {ticker.Bid}");
                    newTrade = CreateTrade(ticker.Market, ticker, _stats.AvailableBTC);
                    _stats.AvailableBTC = 0;
                }
                else if (tradeAction.TradeAdvice == TradeAdvice.Sell)
                {
                    CloseTrade(trade, ticker);
                    var profit = trade.BuyOrder.Price.GetReadablePercentageChange(trade.SellOrder.Price);
                    LogText($"SOLD due to {tradeAction.Reason}\t{ticker.Bid}\t{profit}%\t{ticker.Timestamp:F}\t{trade.SellOrder.Price} BTC\t\t{trade.BuyOrder.Uuid}");
                    //Console.WriteLine($"BC: {trade.BuyOrder.CommissionPaid}\tSC: {trade.SellOrder.CommissionPaid}\tQ: {trade.BuyOrder.Quantity}");
                    //Console.WriteLine($"BP: {trade.BuyOrder.Price}\tSP: {trade.SellOrder.Price}\tSPWC{trade.SellOrder.Price + trade.SellOrder.CommissionPaid}");
                    //Console.WriteLine($"BPP: {trade.BuyOrder.PricePerUnit}\tSPP: {trade.SellOrder.PricePerUnit}");
                    _profitBTC += trade.SellOrder.Price - trade.BuyOrder.Price;
                    _stats.AvailableBTC += Math.Round(trade.SellOrder.Price, 8);
                    newTrade = CreateLowerTrade(ticker);
                }
            }

            if (newTrade != null)
            {
                _trades.Add(newTrade);
            }
        }

        private Trade CreateLowerTrade(Ticker ticker)
        {
            var newTicker = new Ticker
            {
                Ask = ticker.Ask,
                Bid = ticker.Bid * Strategy.Settings.BuyLowerPercentage.ToPercentageMultiplier(),
                Timestamp = ticker.Timestamp,
                Market = ticker.Market
            };
            var newTrade = CreateTrade(ticker.Market, newTicker, _stats.AvailableBTC);
            LogText($"Created trade at {Strategy.Settings.BuyLowerPercentage}% lower than {ticker.Ask} - {newTicker.Ask} for trade {_trades.IndexOf(newTrade)}");
            _stats.AvailableBTC = 0;
            return newTrade;
        }

        private void CloseTrade(Trade trade, Ticker ticker)
        {
            var fullPrice = (ticker.Bid * trade.BuyOrder.Quantity);
            trade.SellOrder = new CryptoOrder();
            trade.SellOrder.Market = trade.Market;
            trade.SellOrder.Closed = DateTime.UtcNow;
            trade.SellOrder.IsClosed = true;
            trade.SellOrder.Limit = ticker.Bid;
            trade.SellOrder.OrderType = CryptoOrderType.LimitBuy;
            trade.SellOrder.PricePerUnit = ticker.Bid;
            trade.SellOrder.CommissionPaid = (trade.BuyOrder.Quantity * Consts.BittrexCommission2 * trade.SellOrder.PricePerUnit).RoundSatoshi();
            trade.SellOrder.Price = Math.Round(fullPrice, 8);
            trade.SellOrder.Quantity = trade.BuyOrder.Quantity;
            trade.SellOrder.Uuid = Guid.NewGuid().ToString();
            trade.IsActive = false;
        }

        private Trade CreateTrade(string market, Ticker ticker, decimal budget)
        {
            var trade = new Trade();
            try
            {
                var price = (budget * Consts.BittrexCommission).RoundSatoshi();

                trade.Market = market;
                trade.CurrentTicker = ticker;
                trade.IsActive = true;
                trade.Strategy = Strategy;
                trade.BuyOrder = new CryptoOrder();
                trade.BuyOrder.Market = market;
                trade.BuyOrder.Closed = DateTime.UtcNow;
                trade.BuyOrder.IsClosed = false;
                trade.BuyOrder.Limit = ticker.Bid;
                trade.BuyOrder.OrderType = CryptoOrderType.LimitBuy;
                trade.BuyOrder.PricePerUnit = ticker.Bid;
                trade.BuyOrder.Quantity = (price / ticker.Bid).RoundSatoshi();
                trade.BuyOrder.CommissionPaid = (trade.BuyOrder.Quantity * Consts.BittrexCommission2 * trade.BuyOrder.PricePerUnit).RoundSatoshi();
                trade.BuyOrder.Price = budget;
                trade.BuyOrder.Uuid = Guid.NewGuid().ToString();
                return trade;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return trade;
            }
        }

        private static Ticker CreateTicker(string market, decimal bid, decimal ask, DateTime timestamp)
        {
            return new Ticker { Market = market, Bid = bid, Ask = ask, Timestamp = timestamp };
        }
    }
}
