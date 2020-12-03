using System;
using DShop.Common.Messages;
using Newtonsoft.Json;

namespace DShop.Api.Messages.Commands.Discount
{
    //Copy Message Object from Discount Service
    [MessageNamespace("discounts")]
    public class CreateDiscount : ICommand
    {
        // Immutable because we don't need to change in traveling lifetime
        // Custom routing key: #.discounts.create_discount
        public Guid Id { get; }
        public Guid CustomerId { get; }
        public string Code { get; }
        public double Percentage { get; }

        [JsonConstructor]
        public CreateDiscount(Guid id, Guid customerId,
            string code, double percentage)
        {
            Id = id;
            CustomerId = customerId;
            Code = code;
            Percentage = percentage;
        }
    }
}