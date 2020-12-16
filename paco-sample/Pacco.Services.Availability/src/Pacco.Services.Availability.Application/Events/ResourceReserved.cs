using System;
using MicroBootstrap.Events;

namespace Pacco.Services.Availability.Application.Events
{
    public class ResourceReserved : IEvent
    {
        public Guid ResourceId { get; }
        public DateTime DateTime { get; }

        public ResourceReserved(Guid resourceId, DateTime dateTime)
            => (ResourceId, DateTime) = (resourceId, dateTime);
    }
}