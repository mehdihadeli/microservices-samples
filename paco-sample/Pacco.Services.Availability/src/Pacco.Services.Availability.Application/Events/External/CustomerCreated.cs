using System;
using MicroBootstrap.Events;
using MicroBootstrap.Messages;

namespace Pacco.Services.Availability.Application.Events.External
{
    [Message("customers")]
    public class CustomerCreated : IEvent
    {
        public Guid CustomerId { get; }

        public CustomerCreated(Guid customerId)
            => CustomerId = customerId;
    }
}