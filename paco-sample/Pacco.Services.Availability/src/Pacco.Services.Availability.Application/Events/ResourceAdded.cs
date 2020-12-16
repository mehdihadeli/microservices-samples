using System;
using MicroBootstrap.Events;

namespace Pacco.Services.Availability.Application.Events
{
    public class ResourceAdded : IEvent
    {
        public Guid ResourceId { get; }

        public ResourceAdded(Guid resourceId)
            => ResourceId = resourceId;
    }
}