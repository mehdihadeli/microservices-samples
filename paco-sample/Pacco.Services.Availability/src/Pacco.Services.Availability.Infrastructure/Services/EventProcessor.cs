using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Pacco.Services.Availability.Application.Services;
using Pacco.Services.Availability.Core.DomainEvents;

namespace Pacco.Services.Availability.Infrastructure.Services
{
    //this abstraction allow us once we decide to extend behavior of mesage broker maybe add some new piece of infrastructure, still the application layer stay the same 
    //not being aware of the additional tolls involved
    internal sealed class EventProcessor : IEventProcessor
    {
        private readonly IMessageBroker _messageBroker;
        private readonly IEventMapper _eventMapper;
        private readonly ILogger<EventProcessor> _logger;
        public EventProcessor(IMessageBroker messageBroker, IEventMapper eventMapper, ILogger<EventProcessor> logger)
        {
            this._logger = logger;
            this._eventMapper = eventMapper;
            this._messageBroker = messageBroker;

        }
        public async Task ProcessAsync(IEnumerable<IDomainEvent> events)
        {
            if (events is null)
            {
                return;
            }

            _logger.LogTrace("Processing domain events...");

            foreach (var @event in events)
            {
                // Handle domain event 

                // by calling IDomainHandler internally like a event dispatcher
            }

            _logger.LogTrace("Processing integration events...");
            var integrationEvents = _eventMapper.MapAll(events);
            await _messageBroker.PublishAsync(integrationEvents);
        }
    }
}