using System.Collections.Generic;
using System.Threading.Tasks;
using MicroBootstrap.Events;

namespace Pacco.Services.Availability.Application.Services
{
    // new abstraction or port for message broker 
    public interface IMessageBroker
    {
         Task PublishAsync(params IEvent[] events);
         Task PublishAsync(IEnumerable<IEvent> events);
    }
}