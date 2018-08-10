using Orleans;

using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Core.Models.Grains
{
    public interface ISubscriptionGrain: IGrainWithStringKey
    {
        Task AddSubscription(WebSubscription subscription);
        Task<List<WebSubscription>> GetAllAsync();
    }
}