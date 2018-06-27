using CryBot.Core.Models;
using CryBot.Core.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using System.Threading.Tasks;

namespace CryBot.Web.Controllers
{
    [Route("api/wallet")]
    public class WalletController : Controller
    {
        private readonly ICryptoApi _cryptoApi;

        public WalletController(ICryptoApi cryptoApi, IOptions<EnvironmentConfig> options)
        {
            _cryptoApi = cryptoApi;
            _cryptoApi.Initialize(options.Value.BittrexApiKey, options.Value.BittrexApiSecret);
        }

        [HttpGet]
        public async Task<IActionResult> GetWallet()
        {
            var walletResponse = await _cryptoApi.GetWalletAsync();
            if (walletResponse.IsSuccessful)
                return Ok(new { wallet = walletResponse.Content, isSuccessful = true});
            return BadRequest(new { walletResponse.ErrorMessage });
        }
    }
}
