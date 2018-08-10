using CryBot.Core.Models;
using CryBot.Core.Models.Grains;

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
