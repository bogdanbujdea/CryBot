using CryBot.Core.Storage;
using CryBot.Core.Exchange.Models;

using System.Threading.Tasks;

namespace CryBot.Core.Notifications
{
    public interface IHubNotifier
    {
        Task UpdateTicker(Ticker ticker);
        Task UpdateTrader(TraderState traderState);
    }
}