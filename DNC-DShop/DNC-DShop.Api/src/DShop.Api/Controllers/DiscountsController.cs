using System;
using System.Threading.Tasks;
using DShop.Api.Messages.Commands.Discount;
using DShop.Api.Services;
using DShop.Common.Mvc;
using DShop.Common.RabbitMq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenTracing;

namespace DShop.Api.Controllers
{
    //https://foreverframe.net/cqrs-i-mikroserwisy-odczyt-danych/
    //API Gateway connect to internal restease and its config for load balancing and choose type of forwarding
    //request to internal services

    
    [AllowAnonymous]
    //BaseController APIGateway add X-Operation X-Resource to response header with Accept method
    public class DiscountsController : BaseController
    {
        private readonly IDiscountsService _discountService;

        public DiscountsController(IBusPublisher busPublisher, ITracer tracer,
        IDiscountsService discountService) : base(busPublisher, tracer)
        {
            this._discountService = discountService;
        }

        //For writes, so creates, updates and deletes (CUD) create a command and publish it to the service bus based on
        //a queue like RabbitMQ.
        [HttpPost]
        public async Task<IActionResult> Post(CreateDiscount command) =>
            await SendAsync(command.BindId(x => x.Id), resourceId: command.Id, resource: "discounts");

        //For reads (GET) forward the HTTP request to the internal API (not publicly exposed) of the particular microservice.
        [HttpGet]
        public async Task<ActionResult<object>> Get(Guid customerId)
        {
            //we could use httpclient for calling internal service
            return await _discountService.FindAsync(customerId);
        }


        [HttpGet("{id:guid}")]
        public async Task<ActionResult<object>> GetDetails(Guid id)
        {
            return Result(await _discountService.GetAsync(id));
        }
    }
}