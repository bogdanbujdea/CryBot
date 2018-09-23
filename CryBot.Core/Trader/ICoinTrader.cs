using CryBot.Core.Storage;
using CryBot.Core.Strategies;
using CryBot.Core.Exchange.Models;

using System.Reactive;
using System.Threading.Tasks;
using System.Reactive.Subjects;
using System.Collections.Generic;

namespace CryBot.Core.Trader
{
    public interface ICoinTrader
    {
        void Initialize(TraderState traderState);
        ISubject<Ticker> PriceUpdated { get; }
        ISubject<CryptoOrder> OrderUpdated { get; }
        ISubject<Trade> TradeUpdated { get; }
        Ticker Ticker { get; set; }
        ITradingStrategy Strategy { get; set; }
        bool IsInTestMode { get; set; }
        List<Candle> Candles { get; set; }
        Task<Unit> UpdatePrice(Ticker ticker);
        Task<Unit> UpdateOrder(CryptoOrder cryptoOrder);
        Task<Budget> FinishTest();
    }
}