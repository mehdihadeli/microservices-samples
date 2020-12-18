using System.Threading.Tasks;
using MicroBootstrap.Commands;
using Pacco.Services.Availability.Application.Exceptions;
using Pacco.Services.Availability.Application.Services;
using Pacco.Services.Availability.Core.Entities;
using Pacco.Services.Availability.Core.Repositories;
using Pacco.Services.Availability.Application.IntegrationEvents;

namespace Pacco.Services.Availability.Application.Commands.Handlers
{
    internal sealed class AddResourceHandler : ICommandHandler<AddResource>
    {
        private readonly IResourcesRepository _repository;
        private readonly IMessageBroker _messageBroker;
        //private readonly IEventMapper _eventMapper;
        private readonly IEventProcessor _eventProcessor;

        public AddResourceHandler(IResourcesRepository repository,
         IMessageBroker messageBroker,
         //IEventMapper eventMapper, 
         IEventProcessor eventProcessor)
        {
            _repository = repository;
            this._messageBroker = messageBroker;
            //this._eventMapper = eventMapper;
            this._eventProcessor = eventProcessor;
        }

        public async Task HandleAsync(AddResource command)
        {
            //in our handler we don't catch our exception because of duplication of code everywhere and in our handler we focus on happy path we dont handle error handling and logging in application layer
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

            // //1) event message will publish to availability exchange, but we don't have any queue that bind this exchange and our event routing key and consume it. but we can create
            // //   an queue on ui and bind our queue to availability exchange and event name routing key (resource_added). now we can get message in queue in ui

            // //2) here it works because we have only one integration event or maybe one domain event here but in some case maybe we should publish multiple events such as we have Some events in reservation and how we can orchestrate this?

            // //3) we have same flow related to publishing, mapping, translating this events from domain event to integration once in each handler which is command handler. so we make a ultimate abstraction where we call one line and pass our 
            // //   our domain object and list our domain events and it take care of translating, mapping, publishing, logging and maybe decorating this with some other stuff,..
            // //await this._messageBroker.PublishAsync(new ResourceAdded(resource.Id));

            //automation of our events handling process contain domain events and external events

            // Solution 1
            // --- Dispatch our domain events , before publish our integration event we need handle our domain  ---
            // foreach (var @event in resource.Events)
            // {
            //   //handling domainevents
            //   // by calling IDomainHandler internally like a event dispatcher
            // }
            // --- translating of the events to Integration Events ---
            // var integrationEvents = _eventMapper.MapAll(resource.Events); // map domain event to integration events
            // await this._messageBroker.PublishAsync(events);


            // Solution 2
            // --- previous approach has many duplicate code for all of our handlers ---
            // so we extract it into a IEventProcessor abstraction and inside in this event process we do all mapping and executing domain events and integration events by one line
            await _eventProcessor.ProcessAsync(resource.Events);

            // for handling exception in rabbitmq level we use ExceptionToMessageMapper
        }
    }
}