using CryBot.Contracts;
using CryBot.Core.Services;

using Orleans;
using Orleans.Providers;

using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Core.Models
{
    [StorageProvider(ProviderName = "OrleansSqlStore")]
    public class TraderGrain : Grain<TraderState>, ITraderGrain
    {
        public async Task UpdatePriceAsync(ITicker ticker)
        {
            await ReadStateAsync();
            State.CurrentTicker = ticker;
            await WriteStateAsync();
        }

        public async Task AddTradeAsync(ITrade trade)
        {
            await ReadStateAsync();
            State.Trades.Add(trade);
            await WriteStateAsync();
        }

        public async Task<List<ITrade>> GetActiveTrades()
        {
            await ReadStateAsync();
            return State.Trades ?? new List<ITrade>();
        }

        public async Task<ITraderSettings> GetSettings()
        {
            await ReadStateAsync();
            return State.Settings ?? TraderSettings.Default;
        }
    }
}