using System;
using System.Threading.Tasks;
using MicroBootstrap.Events;
using MicroBootstrap.MessageBrokers;
using MicroBootstrap.MessageBrokers.Outbox;

namespace Pacco.Services.Availability.Infrastructure.Decorators
{
  public class OutboxEventHandlerDecorator<T> : IEventHandler<T> where T : class, IEvent
    {
        private readonly IMessageOutbox _outbox;
        private readonly IMessagePropertiesAccessor _messagePropertyAccessor;
        private readonly bool _enabled;
        private readonly IEventHandler<T> _handler;
        private readonly string _messageId;

        public OutboxEventHandlerDecorator(IEventHandler<T> handler, IMessageOutbox outbox, IMessagePropertiesAccessor messagePropertyAccessor)
        {
            this._handler = handler;
            this._outbox = outbox;
            this._messagePropertyAccessor = messagePropertyAccessor;
            _enabled = _outbox.Enabled;

            var messageProperties = messagePropertyAccessor.MessageProperties;
            //sometimes messageId property is null because it is a message received from web api not from rabbitmq we can create a random guid
            _messageId = string.IsNullOrWhiteSpace(messageProperties?.MessageId) ? Guid.NewGuid().ToString("N") : messageProperties.MessageId;
        }
        public async Task HandleAsync(T @event)
        {
            if (_enabled)
                await _outbox.HandleAsync(_messageId, () => _handler.HandleAsync(@event));
            else await _handler.HandleAsync(@event);
        }
    }
}