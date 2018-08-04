using CryBot.Core.Models;
using CryBot.Core.Utilities;

using System.Linq;

using System.Threading.Tasks;

using System.Collections.Generic;

namespace CryBot.Core.Services
{
    public class CoinTrader
    {
        private readonly ICryptoApi _cryptoApi;

        public CoinTrader(ICryptoApi cryptoApi)
        {
            _cryptoApi = cryptoApi;
        }

        public void Initialize(string market)
        {
            Market = market;
            Strategy = new HoldUntilPriceDropsStrategy();
        }

        public string Market { get; set; }

        public Ticker Ticker { get; set; }

        public ITradingStrategy Strategy { get; set; }

        public List<Trade> Trades { get; set; } = new List<Trade>();

        public Budget Budget { get; set; } = new Budget();

        public async Task UpdatePrice(Ticker ticker)
        {
            Ticker = ticker;
            await UpdateTrades();
        }

        public async Task StartAsync()
        {
            if (Trades.Count == 0)
            {
                Trades.Add(new Trade());
            }
        }

        public void OrderUpdated(CryptoOrder cryptoOrder)
        {
            switch (cryptoOrder.OrderType)
            {
                case CryptoOrderType.LimitSell:
                    Budget.Available += cryptoOrder.Price;
                    var tradeForSellOrder = Trades.FirstOrDefault(t => t.SellOrder.Uuid == cryptoOrder.Uuid);
                    if (tradeForSellOrder != null)
                    {
                        var tradeProfit = tradeForSellOrder.BuyOrder.Price.GetReadablePercentageChange(tradeForSellOrder.SellOrder.Price);
                        Budget.Profit += tradeProfit;
                        Budget.Earned += tradeForSellOrder.SellOrder.Price - tradeForSellOrder.BuyOrder.Price;
                        tradeForSellOrder.Profit = tradeProfit;
                        tradeForSellOrder.Status = TradeStatus.Completed;
                    }
                    break;
                case CryptoOrderType.LimitBuy:
                    var tradeForBuyOrder = Trades.FirstOrDefault(t => t.BuyOrder.Uuid == cryptoOrder.Uuid);
                    if (tradeForBuyOrder != null)
                    {
                        tradeForBuyOrder.Status = TradeStatus.Bought;
                    }
                    break;
            }
        }

        private async Task UpdateTrades()
        {
            List<Trade> newTrades = new List<Trade>();
            foreach (var trade in Trades.Where(t => t.Status != TradeStatus.Completed))
            {
                var newTrade = await UpdateTrade(trade);
                if (newTrade != Trade.Empty)
                    newTrades.Add(newTrade);
            }

            if (newTrades.Count > 0)
                Trades.AddRange(newTrades);
        }

        private async Task<Trade> UpdateTrade(Trade trade)
        {
            var tradeAction = Strategy.CalculateTradeAction(Ticker, trade);
            switch (tradeAction.TradeAdvice)
            {
                case TradeAdvice.Buy:
                    var buyOrder = await CreateBuyOrder(tradeAction.OrderPricePerUnit);
                    trade.BuyOrder = buyOrder;
                    break;
                case TradeAdvice.Sell:
                    await CreateSellOrder(trade, tradeAction.OrderPricePerUnit);
                    return new Trade { Status = TradeStatus.Empty };
                case TradeAdvice.Cancel:
                    var cancelResponse = await _cryptoApi.CancelOrder(trade.BuyOrder.Uuid);
                    if (cancelResponse.IsSuccessful)
                    {
                        Budget.Available += trade.BuyOrder.Price;
                        trade.Status = TradeStatus.Empty;
                    }
                    break;
            }

            return Trade.Empty;
        }

        private async Task CreateSellOrder(Trade trade, decimal pricePerUnit)
        {
            var sellOrder = new CryptoOrder
            {
                PricePerUnit = pricePerUnit,
                Limit = pricePerUnit,
                OrderType = CryptoOrderType.LimitSell,
                Market = Market,
                Price = pricePerUnit * trade.BuyOrder.Quantity,
                Quantity = trade.BuyOrder.Quantity
            };
            var sellResponse = await _cryptoApi.SellCoinAsync(sellOrder);
            if (sellResponse.IsSuccessful)
            {
                trade.Status = TradeStatus.Selling;
            }
        }

        private async Task<CryptoOrder> CreateBuyOrder(decimal pricePerUnit)
        {
            if (Budget.Available < Strategy.Settings.TradingBudget)
            {
                Budget.Available += Strategy.Settings.TradingBudget;
                Budget.Invested += Strategy.Settings.TradingBudget;
            }
            var priceWithoutCommission = Strategy.Settings.TradingBudget * Consts.BittrexCommission;
            var quantity = priceWithoutCommission / pricePerUnit;
            quantity = quantity.RoundSatoshi();
            var buyOrder = new CryptoOrder
            {
                PricePerUnit = pricePerUnit,
                Price = Strategy.Settings.TradingBudget,
                Quantity = quantity,
                IsOpened = true,
                Market = Market,
                Limit = pricePerUnit,
                OrderType = CryptoOrderType.LimitBuy
            };
            var buyResponse = await _cryptoApi.BuyCoinAsync(buyOrder);
            if (buyResponse.IsSuccessful)
            {
                Budget.Available -= buyResponse.Content.Price;
            }

            return buyResponse.Content;
        }
    }
}
