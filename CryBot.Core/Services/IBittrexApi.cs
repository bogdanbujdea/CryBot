using CryBot.Core.Models;

using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Core.Services
{
    public interface ICryptoApi
    {
        void Initialize(string apiKey, string apiSecret);

        Task<CryptoResponse<Wallet>> GetWalletAsync();

        Task<CryptoResponse<List<CryptoOrder>>> GetOpenOrdersAsync();

        Task<CryptoResponse<List<CryptoOrder>>> GetCompletedOrdersAsync();
    }
}
