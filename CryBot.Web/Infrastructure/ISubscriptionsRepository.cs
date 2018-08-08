using CryBot.Core.Models;

using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Web.Infrastructure
{
    public interface ISubscriptionsRepository
    {
        Task AddSubscription(WebSubscription webSubscription);
        Task<List<WebSubscription>> GetSubscriptionsAsync();
        Task RemoveSubscription(WebSubscription webSubscription);
    }

    class SubscriptionsRepository : ISubscriptionsRepository
    {
        private List<WebSubscription> _webSubscriptions;

        public SubscriptionsRepository()
        {
            _webSubscriptions = new List<WebSubscription>();
        }

        public async Task AddSubscription(WebSubscription webSubscription)
        {
            _webSubscriptions.Add(webSubscription);
        }

        public async Task<List<WebSubscription>> GetSubscriptionsAsync()
        {
            return _webSubscriptions;
        }

        public async Task RemoveSubscription(WebSubscription webSubscription)
        {
            _webSubscriptions.RemoveAt(0);
        }
    }
}