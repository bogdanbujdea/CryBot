using Bittrex.Net.Interfaces;

using CryBot.Core.Models;

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Core.Services
{
    public class FakeBittrexApi : BittrexApi
    {
        public static int BuyOrdersCount = 1;
        public static int SellOrdersCount = 1;
        private int _tickersCount = 1;
        private Ticker _lastTicker = new Ticker { Ask = 1, Bid = -1 };
        private List<CryptoOrder> _pendingBuyOrders = new List<CryptoOrder>();
        private List<CryptoOrder> _pendingSellOrders = new List<CryptoOrder>();

        public FakeBittrexApi(IBittrexClient bittrexClient) : base(bittrexClient)
        {
            MarketsUpdated += MarketsUpdatedHandler;
        }

        private void MarketsUpdatedHandler(object sender, List<Ticker> e)
        {
            _lastTicker = e.FirstOrDefault();
            //Console.WriteLine($"FAKE: {_tickersCount}");
            UpdateBuyOrders();
            UpdateSellOrders();
            _tickersCount++;
        }

        private void UpdateSellOrders()
        {
            List<CryptoOrder> removedOrders = new List<CryptoOrder>();
            foreach (var sellOrder in _pendingSellOrders.Where(s => s.IsClosed == false))
            {
                if (_lastTicker.Bid >= sellOrder.PricePerUnit || sellOrder.OrderType == CryptoOrderType.LimitSell)
                {
                    sellOrder.IsClosed = true;
                    sellOrder.Closed = DateTime.UtcNow;
                    sellOrder.OrderType = CryptoOrderType.LimitSell;
                    removedOrders.Add(sellOrder);
                    OnOrderUpdate(sellOrder);
                }
            }

            foreach (var removedOrder in removedOrders)
            {
                _pendingSellOrders.Remove(removedOrder);
            }
        }

        private void UpdateBuyOrders()
        {
            List<CryptoOrder> removedOrders = new List<CryptoOrder>();
            foreach (var cryptoOrder in _pendingBuyOrders)
            {
                if (_lastTicker.Ask <= cryptoOrder.PricePerUnit)
                {
                    Console.WriteLine($"Closed order {cryptoOrder.Uuid}");
                    cryptoOrder.Closed = DateTime.UtcNow;
                    cryptoOrder.IsClosed = true;
                    OnOrderUpdate(cryptoOrder);
                    removedOrders.Add(cryptoOrder);
                }
                if (cryptoOrder.Canceled)
                    removedOrders.Add(cryptoOrder);
            }

            foreach (var removedOrder in removedOrders)
            {
                _pendingBuyOrders.Remove(removedOrder);
            }
        }

        public override Task<CryptoResponse<CryptoOrder>> BuyCoinAsync(CryptoOrder cryptoOrder)
        {
            cryptoOrder.Uuid = "BUYORDER-" + BuyOrdersCount++;
            cryptoOrder.IsOpened = true;
            _pendingBuyOrders.Add(cryptoOrder);
            return Task.FromResult(new CryptoResponse<CryptoOrder>(cryptoOrder)); ;
        }

        public override Task<CryptoResponse<CryptoOrder>> SellCoinAsync(CryptoOrder sellOrder)
        {
            sellOrder.Uuid = "SELLORDER-" + SellOrdersCount++;
            sellOrder.IsOpened = true;
            _pendingSellOrders.Add(sellOrder);
            return Task.FromResult(new CryptoResponse<CryptoOrder>(sellOrder)); ;
        }
    }
}
