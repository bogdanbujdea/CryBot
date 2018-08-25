using Bittrex.Net.Objects;

using CryBot.Core.Exchange.Models;

using System.Threading.Tasks;
using System.Reactive.Subjects;
using System.Collections.Generic;

namespace CryBot.Core.Exchange
{
    public interface ICryptoApi
    {
        ISubject<CryptoOrder> OrderUpdated { get; }

        ISubject<Ticker> TickerUpdated { get; }

        bool IsInTestMode { get; set; }

        void Initialize(string apiKey, string apiSecret, bool isInTestMode, bool enableStreaming = false);

        Task<CryptoResponse<Wallet>> GetWalletAsync();

        Task<CryptoResponse<List<CryptoOrder>>> GetOpenOrdersAsync();

        Task<CryptoResponse<List<CryptoOrder>>> GetCompletedOrdersAsync();

        Task<CryptoResponse<CryptoOrder>> BuyCoinAsync(CryptoOrder buyOrder);

        Task<CryptoResponse<Ticker>> GetTickerAsync(string market);

        Task<CryptoResponse<CryptoOrder>> SellCoinAsync(CryptoOrder sellOrder);

        Task<CryptoResponse<List<Market>>> GetMarketsAsync();

        Task<CryptoResponse<List<Candle>>> GetCandlesAsync(string market, TickInterval interval);

        Task SendMarketUpdates(string market);

        Task<CryptoResponse<CryptoOrder>> CancelOrder(string orderId);

        Task<CryptoResponse<CryptoOrder>> GetOrderInfoAsync(string uuid);
    }
}