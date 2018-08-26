using CryBot.Core.Notifications;
using Orleans;
using Orleans.Providers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public Task RemoveSubscription(WebSubscription sub)
        {

            if (State.Subscriptions == null)
                State.Subscriptions = new List<WebSubscription>();
            var existingSub = State.Subscriptions.FirstOrDefault(s => s.Id == sub.Id);
            if (existingSub != null)
                State.Subscriptions.Remove(existingSub);
            return WriteStateAsync();
        }
    }
}