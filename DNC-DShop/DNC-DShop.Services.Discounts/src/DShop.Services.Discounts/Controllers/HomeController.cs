using System;
using DShop.Common;
using DShop.Common.Consul;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DShop.Services.Discounts.Controllers
{
    [Route("")]
    public class HomeController : ControllerBase
    {
        private readonly IOptions<AppOptions> _appOptions;
        public HomeController(IOptions<AppOptions> options)
        {
            this._appOptions = options;
        }
        [HttpGet]
        public IActionResult Get() => Ok(_appOptions.Value.Name);

        [HttpGet("ping")]
        public IActionResult Ping() => Ok();
    }
}