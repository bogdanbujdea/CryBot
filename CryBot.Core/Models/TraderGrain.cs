using CryBot.Contracts;
using Orleans;
using Orleans.Providers;

using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Core.Models
{
    [StorageProvider(ProviderName = "OrleansSqlStore")]
    public class TraderGrain : Grain<TraderState>, ITraderGrain
    {
        public async Task UpdatePriceAsync(Ticker ticker)
        {
            await ReadStateAsync();
            State.CurrentTicker = ticker;
            await WriteStateAsync();
        }

        public async Task AddTradeAsync(Trade trade)
        {
            await ReadStateAsync();
            State.Trades.Add(trade);
            await WriteStateAsync();
        }

        public async Task<List<Trade>> GetActiveTrades()
        {
            await ReadStateAsync();
            return State.Trades ?? new List<Trade>();
        }

        public async Task<TraderSettings> GetSettings()
        {
            await ReadStateAsync();
            return State.Settings ?? TraderSettings.Default;
        }

        public async Task<TraderState> GetTraderData()
        {
            await ReadStateAsync();
            return State;
        }

        public async Task UpdateTrades(List<Trade> trades)
        {
            await ReadStateAsync();
            State.Trades = trades;
            await WriteStateAsync();
        }

        public async Task SetMarketAsync(string market)
        {
            await ReadStateAsync();
            State.Market = market;
            await WriteStateAsync();
        }
    }
}