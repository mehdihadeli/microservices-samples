using System.Threading.Tasks;
using MicroBootstrap.Events;
using Microsoft.Extensions.Logging;

namespace Pacco.Services.Availability.Application.IntegrationEvents.External.Handlers
{
    public class CustomerCreatedHandler : IEventHandler<CustomerCreated>
    {
        public ILogger<CustomerCreatedHandler> Logger { get; }
        public CustomerCreatedHandler(ILogger<CustomerCreatedHandler> logger)
        {
            this.Logger = logger;

        }
        // Customer data could be saved into custom DB depending on the business requirements.
        // Given the asynchronous nature of events, this would result in eventual consistency.

        //with command and query handler we have a dispatcher inside our controller and pass message from api layer to handler synchronsly. 
        //but what about event? how do we get the message to this handler from rabbitmq. we need to subscribe to the message to handle this event

        //we can subscribe to this message in UseRabbitMQ.Sunscribe() method for both event and command generic way also SubscribeCommand() and SubscribeEvent because message broker don't any thing about command and event
        //it is message to message.

        //with SubscribeEvent with a event type here 'CustomerCreated' event type
        public Task HandleAsync(CustomerCreated @event)
        {
            Logger.LogDebug($"a customer with id {@event.CustomerId.ToString()} created.");
            return Task.CompletedTask;
        }
    }
}