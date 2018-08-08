using System.Collections.Generic;
using Orleans;

namespace CryBot.Core.Models
{
    public class SubscriptionsState
    {
        public string Id { get; set; }

        public List<WebSubscription> Subscriptions { get; set; }
    }
}