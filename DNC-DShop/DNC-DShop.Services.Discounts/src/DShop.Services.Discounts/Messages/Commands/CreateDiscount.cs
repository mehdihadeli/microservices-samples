using System;
using DShop.Common.Messages;
using Newtonsoft.Json;

namespace DShop.Services.Discounts.Messages.Commands
{
    // Immutable because we don't need to change in traveling lifetime
    // Custom routing key: #.discounts.create_discount
    public class CreateDiscount : ICommand
    {
        //Imutable properties - ReadOnly properties insted of private setter
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