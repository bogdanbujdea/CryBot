using System.Collections.Generic;

namespace CryBot.Core.Models.Grains
{
    public class SubscriptionsState
    {
        public string Id { get; set; }

        public List<WebSubscription> Subscriptions { get; set; }
    }
}