using CryBot.Core.Models;

using System.Threading.Tasks;
using System.Collections.Generic;
using CryBot.Core.Models.Grains;

namespace CryBot.Core.Services
{
    public interface ITradersManager
    {
        Task<List<TraderState>> GetAllTraders();

        Task CreateTraderAsync(string market);
    }
}
