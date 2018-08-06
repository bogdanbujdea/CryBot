using Bittrex.Net.Interfaces;

using CryBot.Core.Models;

using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Core.Services
{
    public class FakeBittrexApi : BittrexApi
    {
        private readonly List<CryptoOrder> _pendingBuyOrders = new List<CryptoOrder>();
        private readonly List<CryptoOrder> _pendingSellOrders = new List<CryptoOrder>();

        public FakeBittrexApi(IBittrexClient bittrexClient) : base(bittrexClient)
        {
            TickerUpdated
                .Where(ticker => ticker.Market == "BTC-ETC")
                .Select(ticker => Observable.FromAsync(token => MarketsUpdatedHandler(ticker)))
                .Concat()
                .Subscribe();
        }

        public override Task<CryptoResponse<CryptoOrder>> BuyCoinAsync(CryptoOrder cryptoOrder)
        {
            //cryptoOrder.Uuid = "BUYORDER-" + BuyOrdersCount++;
            cryptoOrder.IsOpened = true;
            _pendingBuyOrders.Add(cryptoOrder);
            return Task.FromResult(new CryptoResponse<CryptoOrder>(cryptoOrder)); ;
        }

        public override Task<CryptoResponse<CryptoOrder>> SellCoinAsync(CryptoOrder sellOrder)
        {
            //sellOrder.Uuid = "SELLORDER-" + SellOrdersCount++;
            sellOrder.IsOpened = true;
            _pendingSellOrders.Add(sellOrder);
            return Task.FromResult(new CryptoResponse<CryptoOrder>(sellOrder)); ;
        }

        public override Task<CryptoResponse<CryptoOrder>> CancelOrder(string orderId)
        {
            var existingOrder = _pendingBuyOrders.FirstOrDefault(b => b.Uuid == orderId);
            if (existingOrder != null)
            {
                existingOrder.IsOpened = false;
                _pendingBuyOrders.Remove(existingOrder);
            }
            return Task.FromResult(new CryptoResponse<CryptoOrder>(existingOrder)); ;
        }

        private async Task<Unit> MarketsUpdatedHandler(Ticker ticker)
        {
            UpdateBuyOrders(ticker);
            UpdateSellOrders(ticker);
            return await Task.FromResult(Unit.Default);
        }

        private void UpdateSellOrders(Ticker ticker)
        {
            List<CryptoOrder> removedOrders = new List<CryptoOrder>();
            foreach (var sellOrder in _pendingSellOrders.Where(s => s.IsClosed == false))
            {
                if (ticker.Bid >= sellOrder.PricePerUnit || sellOrder.OrderType == CryptoOrderType.ImmediateSell)
                {                    
                    sellOrder.IsClosed = true;
                    sellOrder.IsOpened = false;
                    sellOrder.Closed = ticker.Timestamp;
                    sellOrder.OrderType = CryptoOrderType.LimitSell;
                    removedOrders.Add(sellOrder);
                    OrderUpdated.OnNext(sellOrder);
                }
            }

            foreach (var removedOrder in removedOrders)
            {
                _pendingSellOrders.Remove(removedOrder);
            }
        }

        private void UpdateBuyOrders(Ticker ticker)
        {
            List<CryptoOrder> removedOrders = new List<CryptoOrder>();
            foreach (var buyOrder in _pendingBuyOrders)
            {
                if (ticker.Ask <= buyOrder.PricePerUnit)
                {
                    Console.WriteLine($"Closed buy order {buyOrder.Uuid}");
                    buyOrder.Closed = ticker.Timestamp;
                    buyOrder.IsOpened = false;
                    buyOrder.IsClosed = true;
                    OrderUpdated.OnNext(buyOrder);
                    removedOrders.Add(buyOrder);
                }
                if (buyOrder.Canceled)
                    removedOrders.Add(buyOrder);
            }

            foreach (var removedOrder in removedOrders)
            {
                _pendingBuyOrders.Remove(removedOrder);
            }
        }
    }
}
