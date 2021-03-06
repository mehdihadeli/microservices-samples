using System;
using DShop.Common.Messages;
using Newtonsoft.Json;

namespace DShop.Services.Operations.Messages.Orders.Commands
{
    [MessageNamespace("orders")]
    public class ApproveOrder : ICommand
    {
        public Guid Id { get; }

        [JsonConstructor]
        public ApproveOrder(Guid id)
        {
            Id = id;
        }
    }
}