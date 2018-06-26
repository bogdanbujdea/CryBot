using CryBot.Core.Models;

using System.Threading.Tasks;

namespace CryBot.Core.Services
{
    public interface ICryptoApi
    {
        void Initialize(string apiKey, string apiSecret);

        Task<Wallet> GetWalletAsync();
    }
}
