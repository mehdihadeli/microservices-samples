using System.Threading.Tasks;
using MicroBootstrap.Commands;
using MicroBootstrap.RabbitMq;
using Pacco.Services.Availability.Application.Exceptions;
using Pacco.Services.Availability.Application.Services;
using Pacco.Services.Availability.Core.Entities;
using Pacco.Services.Availability.Core.Repositories;

namespace Pacco.Services.Availability.Application.Commands.Handlers
{
    internal sealed class AddResourceHandler : ICommandHandler<AddResource>
    {
        private readonly IResourcesRepository _repository;
        private readonly IEventProcessor _eventProcessor;

        public AddResourceHandler(IResourcesRepository repository
            //, IEventProcessor eventProcessor
            )
        {
            _repository = repository;
            //_eventProcessor = eventProcessor;
        }


        public async Task HandleAsync(AddResource command)
        {
            //in our handler we dont catch our exception because of duplication of code everywhere and in our handler we focus on happy path we dont handle error handling and logging in application layer
            //we handle this responsibility in infrastructure layer
            
            // whenever we start to add additional logging, exception handling, retry policy, tracing, monitoring, ..., our application handlers will remain the same and all
            // additional cross cutting concerns around them will be kept in different layer (infra) and application logic doesn't change at all.
            
            if (await _repository.ExistsAsync(command.ResourceId))
            {
                // this is not a domain exception because doesn't exist resource is not domain concern, from domain perspective resource is always there

                throw new ResourceAlreadyExistsException(command.ResourceId);
            }
            // use static factory class for create resource
            // here we use guid for our id but we can use snow-flake approach  
            var resource = Resource.Create(command.ResourceId, command.Tags);
            await _repository.AddAsync(resource);
            // await _eventProcessor.ProcessAsync(resource.Events);
         }
    }
}