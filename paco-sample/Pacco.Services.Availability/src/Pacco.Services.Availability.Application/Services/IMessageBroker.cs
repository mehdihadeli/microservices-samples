using System.Collections.Generic;
using System.Threading.Tasks;
using MicroBootstrap.Events;

namespace Pacco.Services.Availability.Application.Services
{
    public interface IMessageBroker
    {
        Task PublishAsync(params IEvent[] events);
        Task PublishAsync(IEnumerable<IEvent> events);
    }
}