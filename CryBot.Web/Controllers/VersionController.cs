using Microsoft.AspNetCore.Mvc;

namespace CryBot.Web.Controllers
{
    [Route("api/version")]
    public class VersionController : Controller
    {
        [HttpGet]
        public string GetVersion()
        {
            return "1.0.0";
        }
    }
}
