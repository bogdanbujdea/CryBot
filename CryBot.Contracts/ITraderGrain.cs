using Orleans;

using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Contracts
{
    public interface ITraderGrain: IGrainWithStringKey
    {
        Task UpdatePriceAsync(ITicker ticker);

        Task AddTradeAsync(ITrade trade);

        Task<List<ITrade>> GetActiveTrades();

        Task<ITraderSettings> GetSettings();
    }
}
