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

        //we can subscribe to this message in UseRabbitMQ.Subscribe() method for both event and command generic way also SubscribeCommand() and SubscribeEvent because message broker don't any thing about command and event
        //it is message to message.

        //with SubscribeEvent with a event type here 'CustomerCreated' event type
        public Task HandleAsync(CustomerCreated @event)
        {

            // save customer with its state in local DB

            // save customer in local db for availability for prevent further request to customer microservice so we can subscribe to CustomerCreated event but we need also subscribe to all the events some how update the customer 
            // state for example CustomerStateChangedEvent so we have to subscribe to this event to listen to change like a integration event so we always have up to date local data for customer because we have event driven architecture. this
            // is a eventually consistency

            // but for some cases we need immediate consistency and we need to have latest data. and above approach because we use queue mechanism we let user and this customer reserve this even though within the customer service its state already
            // changed to suspicious or band or invalid. in this case customer should allow to do reservation only state is valid and we can't rely on subscribe on the message because of its delay. and we want do it synchronously and wait for response
            // and be sure customer data is fresh so because we do it synchronously via http or grpc or web sockets that are end-to-end or point to point. if customer service doesn't respond it is fine for us fail becuase it's more important for us to have
            // consistent data than service always available. and we need a customerId in our ReserveResource command and after that we should change our ReserveResourceHandler
            Logger.LogDebug($"a customer with id {@event.CustomerId.ToString()} created.");
            return Task.CompletedTask;
        }
    }
}