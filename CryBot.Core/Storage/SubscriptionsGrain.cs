using CryBot.Core.Notifications;

using Orleans;
using Orleans.Providers;

using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Core.Storage
{
    [StorageProvider(ProviderName = "OrleansSqlStore")]
    public class SubscriptionsGrain : Grain<SubscriptionsState>, ISubscriptionGrain
    {
        public Task AddSubscription(WebSubscription subscription)
        {
            if (State.Subscriptions == null)
                State.Subscriptions = new List<WebSubscription>();
            State.Subscriptions.Add(subscription);
            return WriteStateAsync();
        }

        public Task<List<WebSubscription>> GetAllAsync()
        {
            return Task.FromResult(State.Subscriptions ?? new List<WebSubscription>());
        }
    }
}