using CryBot.Core.Models;

using Microsoft.Extensions.Options;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using System;
using System.Threading.Tasks;
using CryBot.Core.Services;
using WebPush;

namespace CryBot.Web.Infrastructure
{
    public class PushManager : IPushManager
    {
        private readonly ISubscriptionsRepository _subscriptionsRepository;
        private string _publicKey;
        private string _privateKey;
        private string _subject;
        private VapidDetails _vapidDetails;

        public PushManager(ISubscriptionsRepository subscriptionsRepository, IOptions<EnvironmentConfig> config)
        {
            _subscriptionsRepository = subscriptionsRepository;
            _publicKey = config.Value.PushPublicKey;
            _privateKey = config.Value.PushPrivateKey;
            _subject = @"mailto:test@test.com";
            _vapidDetails = new VapidDetails(_subject, _publicKey, _privateKey);
        }

        public async Task TriggerPush(PushMessage pushMessage)
        {
            pushMessage.Message += $"-{DateTime.Now:dddd HH:mm:ss}";
            var subscriptions = await _subscriptionsRepository.GetSubscriptionsAsync();
            foreach (var sub in subscriptions)
            {
                var subscription = new PushSubscription(sub.Endpoint, sub.Key, sub.AuthSecret);

                var webPushClient = new WebPushClient();
                try
                {
                    webPushClient.SendNotification(subscription, JsonConvert.SerializeObject(pushMessage, new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    }), _vapidDetails);
                }
                catch (WebPushException exception)
                {
                    Console.WriteLine("Http STATUS code" + exception.StatusCode);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    await _subscriptionsRepository.RemoveSubscription(sub);
                }
            }
        }
    }

}
