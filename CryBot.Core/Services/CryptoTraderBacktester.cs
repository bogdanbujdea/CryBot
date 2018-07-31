using CryBot.Contracts;
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

        public ITradingStrategy Strategy { get; set;}

        public CryptoTraderStats StartFromFile(string market)
        {
            _market = market;
            var prices = Candles.Select(c => CreateTicker(_market, c.Low, c.High, c.Timestamp)).ToList();
            _trades.Add(CreateTrade(_market, prices[0], Strategy.Settings.DefaultBudget));
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
            _stats.InvestedBTC += Strategy.Settings.DefaultBudget;
            _stats.CurrentBTC = _stats.InvestedBTC;
            _profitBTC = 0M;
            _newTradeCount = 1;
        }

        private void LogText(string text)
        {
            //Console.WriteLine(text);
        }

        private void GetStats()
        {
            var totalBTC = _trades.Where(t => t.IsActive).Sum(t => t.CurrentValue);
            _stats.Closed = _trades.Count(t => t.IsActive == false);
            _stats.Opened = _trades.Count(t => t.IsActive);
            LogText($"BTC: {_profitBTC.RoundSatoshi()}");
            LogText($"Trade count: {_newTradeCount}");
            LogText($"Diff BTC: {(totalBTC - _stats.InvestedBTC).RoundSatoshi()}");
            LogText($"Triggered buys: {_trades.Count(t => t.TriggeredBuy)}");
            LogText($"Closed trades count: {_stats.Closed}");
            LogText($"Opened trades count: {_trades.Count(t => t.IsActive)}");
            _stats.Profit = _stats.InvestedBTC.GetReadablePercentageChange(_stats.InvestedBTC + _profitBTC);
        }

        private void UpdateTicker(Ticker ticker)
        {
            Trade newTrade = null;
            if (_trades.Count(t => t.IsActive) == 0)
            {
                var newTicker = new Ticker
                {
                    Ask = ticker.Ask * Strategy.Settings.BuyLowerPercentage.ToPercentageMultiplier(),
                    Bid = ticker.Bid,
                    Timestamp = ticker.Timestamp,
                    Market = ticker.Market
                };
                newTrade = CreateTrade(ticker.Market, newTicker, _stats.AvailableBTC);
                _trades.Add(newTrade);
                LogText($"Created trade at {Strategy.Settings.BuyLowerPercentage}% lower than {ticker.Ask} - {newTicker.Ask} for trade {_trades.IndexOf(newTrade)}");
                _stats.AvailableBTC = 0;
                newTrade = null;
            }

            foreach (var trade in _trades.Where(t => t.IsActive))
            {
                if (trade.BuyOrder.IsClosed == false)
                {
                    if (trade.BuyOrder.PricePerUnit >= ticker.Ask)
                    {
                        trade.BuyOrder.Uuid = _trades.IndexOf(trade).ToString();
                        LogText($"BOUGHT\t{trade.BuyOrder.PricePerUnit}\t{ticker.Timestamp:F}\t{trade.BuyOrder.Price} BTC\t\t{trade.BuyOrder.Uuid}");
                        trade.BuyOrder.IsClosed = true;
                    }

                    continue;
                }

                Strategy.CurrentTrade = trade;
                var tradeAction = Strategy.CalculateTradeAction(ticker);
                if (tradeAction.TradeAdvice == TradeAdvice.Buy)
                {
                    _newTradeCount++;
                    ticker.Ask = tradeAction.OrderPricePerUnit;
                    if (_stats.AvailableBTC <= Strategy.Settings.DefaultBudget)
                    {
                        _stats.InvestedBTC += Strategy.Settings.DefaultBudget;
                        _stats.AvailableBTC = Strategy.Settings.DefaultBudget;
                    }

                    newTrade = CreateTrade(ticker.Market, ticker, _stats.AvailableBTC);
                    _stats.AvailableBTC = 0;
                }
                else if (tradeAction.TradeAdvice == TradeAdvice.Sell)
                {
                    CloseTrade(trade, ticker);
                    var profit = trade.BuyOrder.Price.GetReadablePercentageChange(trade.SellOrder.Price);
                    LogText($"SOLD\t{ticker.Bid}\t{profit}%\t{ticker.Timestamp:F}\t{trade.SellOrder.Price} BTC\t\t{trade.BuyOrder.Uuid}");
                    _profitBTC += trade.SellOrder.Price - trade.BuyOrder.Price;
                    _stats.AvailableBTC += Math.Round(trade.SellOrder.Price, 8);
                }
            }

            if (newTrade != null)
            {
                _trades.Add(newTrade);
            }
        }

        private void CloseTrade(Trade trade, Ticker ticker)
        {
            trade.SellOrder = new CryptoOrder
            {
                Market = trade.Market,
                Closed = DateTime.UtcNow,
                IsClosed = true,
                Limit = ticker.Ask,
                CommissionPaid = 0,
                OrderType = CryptoOrderType.LimitBuy,
                PricePerUnit = ticker.Bid,
                Price = Math.Round((ticker.Bid * trade.BuyOrder.Quantity) * Consts.BittrexCommission, 8),
                Quantity = trade.BuyOrder.Quantity,
                Uuid = Guid.NewGuid().ToString()
            };
            trade.IsActive = false;
        }

        private Trade CreateTrade(string market, Ticker ticker, decimal budget)
        {
            LogText($"Created trade at {ticker.Ask} for {budget} BTC\t{ticker.Timestamp:F}\n");
            var trade = new Trade();
            try
            {
                var price = Math.Round(budget * Consts.BittrexCommission, 8);
                var commission = budget - price;
                trade.Market = market;
                trade.CurrentTicker = ticker;
                trade.IsActive = true;
                trade.Strategy = Strategy;
                trade.BuyOrder = new CryptoOrder();
                trade.BuyOrder.Market = market;
                trade.BuyOrder.Closed = DateTime.UtcNow;
                trade.BuyOrder.IsClosed = false;
                trade.BuyOrder.Limit = ticker.Ask;
                trade.BuyOrder.CommissionPaid = commission;
                trade.BuyOrder.OrderType = CryptoOrderType.LimitBuy;
                trade.BuyOrder.PricePerUnit = ticker.Ask;
                trade.BuyOrder.Quantity = Math.Round(price / ticker.Ask, 8);
                trade.BuyOrder.Price = price;
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
