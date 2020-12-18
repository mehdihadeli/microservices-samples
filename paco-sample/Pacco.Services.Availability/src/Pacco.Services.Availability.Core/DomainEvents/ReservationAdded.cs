using Pacco.Services.Availability.Core.Entities;
using Pacco.Services.Availability.Core.ValueObjects;

namespace Pacco.Services.Availability.Core.DomainEvents
{
    // we put our domain events in core layer
    
    // we can put whole aggregate here because will not leak outside of domain and we can handle it inside of our domain and application layer
    public class ReservationAdded : IDomainEvent
    {
        public Resource Resource { get; }
        public Reservation Reservation { get; }

        public ReservationAdded(Resource resource, Reservation reservation)
            => (Resource, Reservation) = (resource, reservation);
    }
}