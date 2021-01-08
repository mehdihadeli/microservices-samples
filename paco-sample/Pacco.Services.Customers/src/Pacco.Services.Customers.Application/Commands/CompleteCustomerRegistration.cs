using System;
using Convey.CQRS.Commands;

namespace Pacco.Services.Customers.Application.Commands
{
    [Contract]
    public class CompleteCustomerRegistration : ICommand
    {
        // this customerId is equal to userId but just for complete registration and customer created before in external event handler SignedUpHandler in customer service that triggered by Identity service
        public Guid CustomerId { get; }
        public string FullName { get; }
        public string Address { get; }

        public CompleteCustomerRegistration(Guid customerId, string fullName, string address)
        {
            CustomerId = customerId;
            FullName = fullName;
            Address = address;
        }
    }
}