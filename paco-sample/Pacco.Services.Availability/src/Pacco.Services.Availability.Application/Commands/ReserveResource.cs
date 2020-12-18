using System;
using MicroBootstrap.Commands;

namespace Pacco.Services.Availability.Application.Commands
{
    // https://enterprisecraftsmanship.com/posts/cqrs-commands-part-domain-model/
    
    // we put the in application layer because they are prt of our usecase 

    // our intention and should be imperative and immutable object

    [Contract]
    public class ReserveResource : ICommand
    {
        public Guid ResourceId { get; }
        public DateTime DateTime { get; }
        public int Priority { get; }
        public Guid CustomerId { get; }

        public ReserveResource(Guid resourceId, DateTime dateTime, int priority, Guid customerId)
            => (ResourceId, DateTime, Priority, CustomerId) = (resourceId, dateTime, priority, customerId);
    }
}