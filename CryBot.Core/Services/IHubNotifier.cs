using CryBot.Contracts;
using System.Threading.Tasks;

namespace CryBot.Core.Services
{
    public interface IHubNotifier
    {
        Task UpdateTicker(Ticker ticker);
        Task UpdateTrader(TraderState traderState);
    }
}