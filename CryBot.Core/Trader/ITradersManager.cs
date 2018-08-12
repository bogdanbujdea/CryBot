using CryBot.Core.Storage;

using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Core.Trader
{
    public interface ITradersManager
    {
        Task<List<TraderState>> GetAllTraders();

        Task CreateTraderAsync(string market);
    }
}
