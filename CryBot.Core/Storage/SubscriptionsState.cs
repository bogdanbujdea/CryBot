using CryBot.Core.Notifications;

using System.Collections.Generic;

namespace CryBot.Core.Storage
{
    public class SubscriptionsState
    {
        public string Id { get; set; }

        public List<WebSubscription> Subscriptions { get; set; }
    }
}