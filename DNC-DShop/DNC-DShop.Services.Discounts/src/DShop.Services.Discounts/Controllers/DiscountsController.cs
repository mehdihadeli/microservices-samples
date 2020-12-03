using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DShop.Common.Dispatchers;
using DShop.Common.Mvc;
using DShop.Services.Discounts.Dto;
using DShop.Services.Discounts.Messages.Commands;
using DShop.Services.Discounts.Queries;
using Microsoft.AspNetCore.Mvc;

namespace DShop.Services.Discounts.Controllers
{
    //Internal Controller
    //we usually expose this api from APIGateway insted of calling our service directly and some other stuff with
    //Gateway like authentication,authorization calling internal services
    [Route("[controller]")]
    [ApiController]
    public class DiscountsController : ControllerBase
    {
        private readonly IDispatcher _dispatcher;

        public DiscountsController(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        // Idempotent : we can call this query many time and we get same result
        // No side effects
        // Doesn't mutate a state
        //We use ActionResult for swagger detect output type
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DiscountDto>>> Get([FromQuery] FindDiscounts query)
            => Ok(await _dispatcher.QueryAsync(query));

        
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<DiscountDetailsDto>> Get([FromRoute] GetDiscount query)
        {
            //throw new ArgumentException("Seq Log Test");
            var discount = await _dispatcher.QueryAsync(query);
            if (discount is null)
            {
                return NotFound();
            }

            return discount;
        }
 
        //we don't use post with this signature because it call synchronously and wait for response.
        //insted we use APIGateway and send to message bus and handle it with handlers but we use httpget in this 
        //controller for fetch data
        [HttpPost]
        public async Task<ActionResult> Post(CreateDiscount command)
        {
            //command is immutable most cases but here changed id
            //dispatcher work in-memory like MediateR and don't use event bus
            await _dispatcher.SendAsync(command.BindId(c => c.Id));

            return Accepted();
        }

    }
}