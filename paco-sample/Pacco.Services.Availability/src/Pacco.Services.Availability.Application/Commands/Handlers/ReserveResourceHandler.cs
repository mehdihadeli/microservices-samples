using System.Threading.Tasks;
using MicroBootstrap.Commands;
using Pacco.Services.Availability.Application.Exceptions;
using Pacco.Services.Availability.Application.Services;
using Pacco.Services.Availability.Core.Repositories;
using Pacco.Services.Availability.Core.ValueObjects;

namespace Pacco.Services.Availability.Application.Commands.Handlers
{
    internal sealed class ReserveResourceHandler : ICommandHandler<ReserveResource>
    {
        private readonly IResourcesRepository _repository;
        private readonly IEventProcessor _eventProcessor;

        public ReserveResourceHandler(IResourcesRepository repository, IEventProcessor eventProcessor)
        {
            _repository = repository;
            this._eventProcessor = eventProcessor;
        }

        // handler is nothing more orchestration of our domain and domain logic and external world (Application Service)
        public async Task HandleAsync(ReserveResource command)
        {
            // whenever we start to add additional logging, exception handling, retry policy, tracing, monitoring, ..., our application handlers will remain the same and all
            // additional cross cutting concerns around them will be kept in different layer (infra) and application logic doesn't change at all.

            var resource = await _repository.GetAsync(command.ResourceId); // get resource document from db and map it to tour domain model with its version
            if (resource is null)
            {
                // this is an application exception not domain exception because this is outside of boundary of particular aggregate and in aggregate perspective aggregate always exists

                // our exception will handle by our exception middleware and ExceptionToResponseMapper
                throw new ResourceNotFoundException(command.ResourceId);
            }
            var reservation = new Reservation(command.DateTime, command.Priority);
            resource.AddReservation(reservation);

             //whatever we have different infrastructure pieces such as mongo, redis, message broker we can never 100 percent sure, because we can't make a transaction on top of thease infra parts
             //so there is a chance might be happen once per month or year

            //when we have different infrastructure pieces, we can never be sure that this will be process transactionally so will need to look for other patterns - outbox pattern
            await _repository.UpdateAsync(resource); //database
            await _eventProcessor.ProcessAsync(resource.Events); //rabbit
        }
    }
}