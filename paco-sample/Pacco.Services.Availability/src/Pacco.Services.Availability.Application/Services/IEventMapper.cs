using System.Collections.Generic;
using MicroBootstrap.Events;
using Pacco.Services.Availability.Core.DomainEvents;

namespace Pacco.Services.Availability.Application.Services
{
    // we create a port in application level because integration events are bounded to this particular layer
    public interface IEventMapper
    {
        IEnumerable<IEvent> MapAll(IEnumerable<IDomainEvent> @events);
        IEvent Map(IDomainEvent @event);
    }
}