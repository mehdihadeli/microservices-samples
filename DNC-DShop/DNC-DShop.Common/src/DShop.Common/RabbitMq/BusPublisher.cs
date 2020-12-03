using System.Threading.Tasks;
using DShop.Common.Messages;
using RawRabbit;
using RawRabbit.Enrichers.MessageContext;

namespace DShop.Common.RabbitMq
{
    public class BusPublisher : IBusPublisher
    {
        private readonly IBusClient _busClient;

        public BusPublisher(IBusClient busClient)
        {
            _busClient = busClient;
        } 
        //UseMessageContext is part of RawRabbit that and it serialize as a header in RabbitMQ properties
        //ICorrelationContext is metadata comes with message and use in message flow and we will not mutate this
        //ICorrelationContext and we create this object in the begining of inside APIGateway and let it go with the
        //messages and use this ICorrelationContext class in message handlers as a second parameter
         public async Task SendAsync<TCommand>(TCommand command, ICorrelationContext context)
            where TCommand : ICommand
            => await _busClient.PublishAsync(command, ctx => ctx.UseMessageContext(context));

        public async Task PublishAsync<TEvent>(TEvent @event, ICorrelationContext context)
            where TEvent : IEvent
            => await _busClient.PublishAsync(@event, ctx => ctx.UseMessageContext(context));
    }
}