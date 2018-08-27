using System.Collections.Generic;
using System.Threading.Tasks;
using CryBot.Core.Exchange.Models;

namespace CryBot.Core.Storage
{
    public interface ITradersRepository
    {
        Task<CryptoResponse<List<Market>>> GetTradedMarketsAsync();

        Task CreateTraderAsync(Market market);
    }
}