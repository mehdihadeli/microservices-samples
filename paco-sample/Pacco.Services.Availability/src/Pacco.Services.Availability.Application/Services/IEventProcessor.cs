using System.Collections.Generic;
using System.Threading.Tasks;
using Pacco.Services.Availability.Core.DomainEvents;

namespace Pacco.Services.Availability.Application.Services
{
    //this is responsible for processing this as a domain events and translating this to integration event, logging, additional tracing and we hide them behind this abstraction 
    public interface IEventProcessor
    {
        //it works in top of using aggregate root
        Task ProcessAsync(IEnumerable<IDomainEvent> events);
    }
}