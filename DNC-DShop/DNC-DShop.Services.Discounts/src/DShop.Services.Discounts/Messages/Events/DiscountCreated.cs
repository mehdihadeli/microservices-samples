using System;
using DShop.Common.Messages;
using Newtonsoft.Json;

namespace DShop.Services.Discounts.Messages.Events
{
    //Events Represent somthins that's happend
    // Immutable
    //Usually whatever exist in command we use in events and add some properties if required
    public class DiscountCreated : IEvent
    {
        public Guid Id { get; }
        public Guid CustomerId { get; }
        public string Code { get; }
        public double Percentage { get; }

        [JsonConstructor]
        public DiscountCreated(Guid id, Guid customerId,
            string code, double percentage)
        {
            Id = id;
            CustomerId = customerId;
            Code = code;
            Percentage = percentage;
        }
    }
}