using System.Threading.Tasks;
using CryBot.Core.Models;

namespace CryBot.Core.Services
{
    public interface IPushManager
    {
        Task TriggerPush(PushMessage pushMessage);
    }
}