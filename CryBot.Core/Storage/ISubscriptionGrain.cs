using CryBot.Core.Notifications;

using Orleans;

using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Core.Storage
{
    public interface ISubscriptionGrain: IGrainWithStringKey
    {
        Task AddSubscription(WebSubscription subscription);
        Task<List<WebSubscription>> GetAllAsync();
        Task RemoveSubscription(WebSubscription sub);
    }
}