using Bittrex.Net.Objects;

using CryBot.Core.Models;
using CryBot.Core.Services;
using CryBot.Web.Infrastructure;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using System.Threading.Tasks;

namespace CryBot.Web.Controllers
{
    [Route("api/wallet")]
    public class WalletController : Controller
    {
        private readonly ICryptoApi _cryptoApi;
        private readonly IPushManager _pushManager;

        public WalletController(ICryptoApi cryptoApi, IOptions<EnvironmentConfig> options, IPushManager pushManager)
        {
            _cryptoApi = cryptoApi;
            _pushManager = pushManager;
            _cryptoApi.Initialize(options.Value.BittrexApiKey, options.Value.BittrexApiSecret, options.Value.TestMode);
        }

        [HttpGet]
        public async Task<IActionResult> GetWallet()
        {
            await _pushManager.TriggerPush(new PushMessage("Hello", "BTC", OrderBookType.Buy));
            var walletResponse = await _cryptoApi.GetWalletAsync();
            if (walletResponse.IsSuccessful)
                return Ok(new { wallet = walletResponse.Content, isSuccessful = true});
            return BadRequest(new { walletResponse.ErrorMessage });
        }
    }
}
