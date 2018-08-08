using Bittrex.Net.Objects;

using CryBot.Core.Models;
using CryBot.Core.Services;
using CryBot.Web.Infrastructure;

using Microsoft.AspNetCore.Mvc;

using Orleans;

using System.Threading.Tasks;

namespace CryBot.Web.Controllers
{
    [Route("api/notifications")]
    public class NotificationsController : Controller
    {
        private readonly IPushManager _pushManager;
        private readonly ISubscriptionsRepository _subscriptionsRepository;
        private readonly IClusterClient _clusterClient;

        public NotificationsController(IPushManager pushManager,
            ISubscriptionsRepository subscriptionsRepository, IClusterClient clusterClient)
        {
            _pushManager = pushManager;
            _subscriptionsRepository = subscriptionsRepository;
            _clusterClient = clusterClient;
        }

        [HttpPost]
        public async Task<IActionResult> AddSubscription([FromBody] WebSubscription webSubscription)
        {
            await _subscriptionsRepository.AddSubscription(webSubscription);
            var pushMessage = new PushMessage("Hello, this is a test!", "BTC", OrderBookType.Buy);
            await _pushManager.TriggerPush(pushMessage);
            return Ok(new { ok = true });
        }
    }
}
