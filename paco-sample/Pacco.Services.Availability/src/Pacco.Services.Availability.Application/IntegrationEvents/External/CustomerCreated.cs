using System;
using MicroBootstrap.Events;
using MicroBootstrap.MessageBrokers;

namespace Pacco.Services.Availability.Application.IntegrationEvents.External
{
    //since integration events will be part of our usecase or part of our application logic so we can keep them in application layer and to distinguish the messages that we publish and which we want to subscribe
    //we create additional directory external actually external folder are incoming messages

    //for receiving an integration event from other service we use external folder, and for sending an outgoing integration event we don't put it in external folder that are incoming messages

    //event should be immutable

    //when we publish a message to customer exchange of customer service and direct to customer queue of availability service that bind to this customer service exchange

    //in out local contract we can mark this message for overwrite some of properties in message attribtue and first parameter is exchange name but also we can specify routing key, queue name,...
    //because in each service default exchange name that defined in setting is the same name of service. routing key will generate automatically from message name to snake case. but we can overwrite it here
    [Message("customers")]
    public class CustomerCreated : IEvent
    {
        public Guid CustomerId { get; }

        public CustomerCreated(Guid customerId)
            => CustomerId = customerId;
    }
}