using System;
using System.Threading.Tasks;
using MicroBootstrap.Commands.Dispatchers;
using MicroBootstrap.Queries.Dispatchers;
using Microsoft.AspNetCore.Mvc;
using Pacco.Services.Availability.Application.Commands;
using Pacco.Services.Availability.Application.DTO;
using Pacco.Services.Availability.Application.Queries;

namespace Pacco.Services.Availability.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResourcesController : ControllerBase
    {
        public IQueryDispatcher QueryDispatcher { get; }
        public ICommandDispatcher CommandDispatcher { get; }
        public ResourcesController(ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher)
        {
            this.CommandDispatcher = commandDispatcher;
            this.QueryDispatcher = queryDispatcher;

        }

        [HttpGet("{resourceId}")]
        public async Task<ActionResult<ResourceDto>> Get([FromRoute] GetResource query)
        {
            var resource = await QueryDispatcher.QueryAsync(query);

            if (resource is { }) // resource is not null
            {
                return resource;
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<ActionResult> Post(AddResource command)
        {
            await CommandDispatcher.SendAsync(command);
            return Created($"api/resources/{command.ResourceId}", null);
        }
    }
}