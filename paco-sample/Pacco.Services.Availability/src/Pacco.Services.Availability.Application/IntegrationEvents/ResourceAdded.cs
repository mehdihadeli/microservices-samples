using System;
using MicroBootstrap.Events;

namespace Pacco.Services.Availability.Application.IntegrationEvents
{

    // this is not a outgoing message so we don't put in external folder, for receiving an integration event from other service we use external folder that are incoming messages
    
    [Contract]
    public class ResourceAdded : IEvent
    {
        public Guid ResourceId { get; }

        public ResourceAdded(Guid resourceId)
            => ResourceId = resourceId;
    }
}