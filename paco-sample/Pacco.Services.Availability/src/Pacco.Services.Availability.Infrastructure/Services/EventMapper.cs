using System.Collections.Generic;
using System.Linq;
using MicroBootstrap.Events;
using Pacco.Services.Availability.Application.IntegrationEvents;
using Pacco.Services.Availability.Application.Services;
using Pacco.Services.Availability.Core.DomainEvents;

namespace Pacco.Services.Availability.Infrastructure.Services
{
    //adapter for IEventMapper port
    internal sealed class EventMapper : IEventMapper
    {
        public IEvent Map(IDomainEvent @event)
        {
            //if we don't want to publish one to one domain event and integration event we can do it ourself by our strategy
            return @event switch
            {
                ResourceCreated e => new ResourceAdded(e.Resource.Id), // domain event --> integration event
                ReservationAdded e => new ResourceReserved(e.Resource.Id, e.Reservation.DateTime),
                ReservationCanceled e => new ResourceReservationCanceled(e.Resource.Id, e.Reservation.DateTime),
                _ => null
            };
        }

        public IEnumerable<IEvent> MapAll(IEnumerable<IDomainEvent> events) => events?.Select(Map);
    }
}