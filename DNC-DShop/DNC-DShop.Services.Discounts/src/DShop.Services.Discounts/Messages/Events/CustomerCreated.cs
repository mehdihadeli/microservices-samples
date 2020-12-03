using System;
using DShop.Common.Messages;
using Newtonsoft.Json;

namespace DShop.Services.Discounts.Messages.Events
{
    //we take some of properties of original CustomerCreated event not all of properties 
    [MessageNamespace("customers")]
    public class CustomerCreated : IEvent
    {
        public Guid Id { get; }
        public string Email { get; }

        [JsonConstructor]
        public CustomerCreated(Guid id, string email)
        {
            Id = id;
            Email = email;
        }
    }
}