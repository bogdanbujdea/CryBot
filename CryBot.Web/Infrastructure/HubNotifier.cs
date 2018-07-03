using CryBot.Contracts;
using CryBot.Core.Hubs;
using CryBot.Core.Services;

using Microsoft.AspNetCore.SignalR;

using System.Threading.Tasks;

namespace CryBot.Web.Infrastructure
{
    public class HubNotifier: IHubNotifier
    {
        private readonly IHubContext<ApplicationHub> _hub;

        public HubNotifier(IHubContext<ApplicationHub> hub)
        {
            _hub = hub;
        }
        public async Task SendTicker(ITicker ticker)
        {
            await _hub.Clients.All.SendAsync("priceUpdate", ticker);
        }
    }
}