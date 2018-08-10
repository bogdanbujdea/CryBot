using CryBot.Core.Models;

using System.Threading.Tasks;

namespace CryBot.Core.Services
{
    public interface IPushManager
    {
        Task TriggerPush(PushMessage pushMessage);
    }
}