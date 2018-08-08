using CryBot.Core.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Web.Infrastructure
{
    public interface ISubscriptionsRepository
    {
        Task AddSubscription(WebSubscription webSubscription);
        Task<List<WebSubscription>> GetSubscriptionsAsync();
    }
}