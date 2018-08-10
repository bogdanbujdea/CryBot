using CryBot.Core.Models;
using CryBot.Core.Models.Grains;

using System.Threading.Tasks;

namespace CryBot.Core.Services
{
    public interface IHubNotifier
    {
        Task UpdateTicker(Ticker ticker);
        Task UpdateTrader(TraderState traderState);
    }
}