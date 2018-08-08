using Bittrex.Net.Objects;
using CryBot.Core.Models;
using CryBot.Core.Services;
using CryBot.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace CryBot.Web.Controllers
{
    [Route("api/notifications")]
    public class NotificationsController : Controller
    {
        private readonly IPushManager _pushManager;
        private readonly ISubscriptionsRepository _subscriptionsRepository;

        public NotificationsController(IPushManager pushManager, ISubscriptionsRepository subscriptionsRepository)
        {
            _pushManager = pushManager;
            _subscriptionsRepository = subscriptionsRepository;
        }

        [HttpPost]
        public IActionResult AddSubscription([FromBody] WebSubscription webSubscription)
        {
            _subscriptionsRepository.AddSubscription(webSubscription);
            var pushMessage = new PushMessage("Hello, this is a test!", "BTC", OrderBookType.Buy);
            _pushManager.TriggerPush(pushMessage);
            return Ok(new { ok = true });
        }
    }
}
