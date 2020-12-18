using System.Collections.Generic;
using System.Linq;
using Pacco.Services.Availability.Core.DomainEvents;

namespace Pacco.Services.Availability.Core.Entities
{
    public abstract class AggregateRoot
    {
        private readonly List<IDomainEvent> _events = new List<IDomainEvent>();
        public IEnumerable<IDomainEvent> Events => _events; // we don't want to expose collection and we make it immutable. because order of event is matter we use list else we use ISet, Hashset
        public AggregateId Id { get; protected set; }

        // we need a version for aggregate for optimistic concurrency or when we modify or mess up its internal state we want increase 
        // the version. how we can increase version? we can do it with inherited classes but default way of our incremneting is adding some domain event.
        public int Version { get; protected set; } 

        //events already happend in system
        protected void AddEvent(IDomainEvent @event)
        {
            if (!_events.Any()) // there is no event, we change or version for first event not for adding all events because domainevent will be persisted we just want to something already changed and we won't anymore.
            {
                Version++;
            }

            _events.Add(@event);
        }

        public void ClearEvents() => _events.Clear();
    }
}