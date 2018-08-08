using CryBot.Core.Models;

using System.Threading.Tasks;
using CryBot.Core.Models.Grains;

namespace CryBot.Core.Services
{
    public interface IHubNotifier
    {
        Task UpdateTicker(Ticker ticker);
        Task UpdateTrader(TraderState traderState);
    }
}