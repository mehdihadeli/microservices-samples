using System.Threading.Tasks;
using MicroBootstrap.Commands;
using Pacco.Services.Availability.Application.Exceptions;
using Pacco.Services.Availability.Application.Services;
using Pacco.Services.Availability.Application.Services.Clients;
using Pacco.Services.Availability.Core.Repositories;
using Pacco.Services.Availability.Core.ValueObjects;

namespace Pacco.Services.Availability.Application.Commands.Handlers
{
    internal sealed class ReserveResourceHandler : ICommandHandler<ReserveResource>
    {
        private readonly IResourcesRepository _repository;
        private readonly ICustomersServiceClient _customersServiceClient;
        private readonly IEventProcessor _eventProcessor;
        private readonly IAppContext _appContext;

        public ReserveResourceHandler(IResourcesRepository repository//,
            //ICustomersServiceClient customersServiceClient,
            //IEventProcessor eventProcessor,
            //IAppContext appContext
            )
        {
            _repository = repository;
            // _customersServiceClient = customersServiceClient;
            // _eventProcessor = eventProcessor;
            //_appContext = appContext;
        }

        // handler is nothing more orchestration of our domain and domain logic and external world (Application Service)
        public async Task HandleAsync(ReserveResource command)
        {
            // var identity = _appContext.Identity;
            // if (identity.IsAuthenticated && identity.Id != command.CustomerId && !identity.IsAdmin)
            // {
            //     throw new UnauthorizedResourceAccessException(command.ResourceId, identity.Id);
            // }

            // whenever we start to add additional logging, exception handling, retry policy, tracing, monitoring, ..., our application handlers will remain the same and all
            // additional cross cutting concerns around them will be kept in different layer (infra) and application logic doesn't change at all.
            
            var resource = await _repository.GetAsync(command.ResourceId); // get resource document from db and map it to tour domain model with its version
            if (resource is null)
            {
                // this is an application exception not domain exception because this is outside of boundary of particular aggregate and in aggregate perspective aggregate always exists

                // our exception will handle by our exception middleware and ExceptionToResponseMapper
                throw new ResourceNotFoundException(command.ResourceId);
            }

            // var customerState = await _customersServiceClient.GetStateAsync(command.CustomerId);
            // if (customerState is null)
            // {
            //     throw new CustomerNotFoundException(command.CustomerId);
            // }

            // if (!customerState.IsValid)
            // {
            //     throw new InvalidCustomerStateException(command.ResourceId, customerState?.State);
            // }

            var reservation = new Reservation(command.DateTime, command.Priority);
            resource.AddReservation(reservation);
            await _repository.UpdateAsync(resource); //update our aggregate
            //await _eventProcessor.ProcessAsync(resource.Events);
        }
    }
}