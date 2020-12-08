using Pacco.Services.Availability.Core.Entities;

namespace Pacco.Services.Availability.Core.Events
{
    // because this is a domain event we can pass whole aggregate inside there is no domain leak because we
    // only dispatch this event within microservice
    public class ResourceCreated : IDomainEvent
    {
        public Resource Resource { get; }

        public ResourceCreated(Resource resource) => Resource = resource;
    }
}