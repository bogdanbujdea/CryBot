using CryBot.Core.Storage;
using CryBot.Core.Notifications;

using Orleans;

using System;

using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Web.Infrastructure
{
    public class SubscriptionsRepository : ISubscriptionsRepository
    {
        private readonly IClusterClient _clusterClient;
        private List<WebSubscription> _webSubscriptions;

        public SubscriptionsRepository(IClusterClient clusterClient)
        {
            _clusterClient = clusterClient;
            _webSubscriptions = new List<WebSubscription>();
        }

        public async Task AddSubscription(WebSubscription webSubscription)
        {
            var subscriptionGrain = _clusterClient.GetGrain<ISubscriptionGrain>("subs");
            await subscriptionGrain.AddSubscription(webSubscription);
        }

        public async Task<List<WebSubscription>> GetSubscriptionsAsync()
        {
            try
            {
                
                var subscriptionGrain = _clusterClient.GetGrain<ISubscriptionGrain>("subs");
                return await subscriptionGrain.GetAllAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return new List<WebSubscription>();
        }

        public async Task RemoveSubscription(WebSubscription sub)
        {
            var subscriptionGrain = _clusterClient.GetGrain<ISubscriptionGrain>("subs");
            await subscriptionGrain.RemoveSubscription(sub);
        }
    }
}