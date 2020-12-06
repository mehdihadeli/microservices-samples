using System.Collections.Generic;
using MicroBootstrap.Events;
using Pacco.Services.Availability.Core.Events;

namespace Pacco.Services.Availability.Application.Services
{
    public interface IEventMapper
    {
        IEvent Map(IDomainEvent @event);
        IEnumerable<IEvent> MapAll(IEnumerable<IDomainEvent> events);
    }
}