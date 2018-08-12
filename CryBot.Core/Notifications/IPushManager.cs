using System.Threading.Tasks;

namespace CryBot.Core.Notifications
{
    public interface IPushManager
    {
        Task TriggerPush(PushMessage pushMessage);
    }
}