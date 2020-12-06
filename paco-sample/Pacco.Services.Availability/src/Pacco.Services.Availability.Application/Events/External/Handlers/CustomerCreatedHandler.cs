using System.Threading.Tasks;
using MicroBootstrap.Events;
using MicroBootstrap.RabbitMq;

namespace Pacco.Services.Availability.Application.Events.External.Handlers
{
    public class CustomerCreatedHandler : IEventHandler<CustomerCreated>
    {
        // Customer data could be saved into custom DB depending on the business requirements.
        // Given the asynchronous nature of events, this would result in eventual consistency.
        public Task HandleAsync(CustomerCreated @event)
        {
           return Task.CompletedTask;
        }
    }
}