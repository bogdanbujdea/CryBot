using CryBot.Contracts;

using System.Threading.Tasks;

namespace CryBot.Core.Services
{
    public interface IHubNotifier
    {
        Task SendTicker(ITicker ticker);
    }
}