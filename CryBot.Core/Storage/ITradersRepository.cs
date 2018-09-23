using CryBot.Core.Exchange.Models;

using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Core.Storage
{
    public interface ITradersRepository
    {
        Task<CryptoResponse<List<Market>>> GetTradedMarketsAsync();

        Task CreateTraderAsync(Market market);
    }
}