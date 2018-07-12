using CryBot.Contracts;

using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Core.Services
{
    public interface ITradersManager
    {
        Task<List<TraderState>> GetAllTraders();

        Task CreateTraderAsync(string market);
    }
}
