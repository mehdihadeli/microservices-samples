using System;
using System.Threading.Tasks;
using MicroBootstrap.Commands;
using MicroBootstrap.MessageBrokers;
using MicroBootstrap.MessageBrokers.Outbox;

namespace Pacco.Services.Availability.Infrastructure.Decorators
{
    //
    public class OutboxCommandHandlerDecorator<T> : ICommandHandler<T> where T : class, ICommand
    {
        private readonly IMessageOutbox _outbox;
        private readonly IMessagePropertiesAccessor _messagePropertyAccessor;
        private readonly bool _enabled;
        private readonly ICommandHandler<T> _handler;
        private readonly string _messageId;

        public OutboxCommandHandlerDecorator(ICommandHandler<T> handler, IMessageOutbox outbox, IMessagePropertiesAccessor messagePropertyAccessor)
        {
            this._handler = handler;
            this._outbox = outbox;
            this._messagePropertyAccessor = messagePropertyAccessor;
            _enabled = _outbox.Enabled;

            var messageProperties = messagePropertyAccessor.MessageProperties;
            //sometimes messageId property is null because it is a message received from web api not from rabbitmq we can create a random guid
            //or we can bypass using of outbox for web api calls
            _messageId = string.IsNullOrWhiteSpace(messageProperties?.MessageId) ? Guid.NewGuid().ToString("N") : messageProperties.MessageId;
        }
        // handling inbox for check unique message
        public async Task HandleAsync(T command)
        {
            //we want to verification of unique messageId somewhere. before we invoke handler we like to verify whether we can invoke this handler or no
            //by checking this message is unique or not. we need some middleware before hit our handler. for example in rabbitmq BusSubscriber before do invocation handle(serviceProvider,message,messagecontext) we like to verify before call handle but if we can't
            //change internal of some library we could put a decorator on top of it. 
            if (_enabled)
                await _outbox.HandleAsync(_messageId, () => _handler.HandleAsync(command)); //here we check unique message. if messageId already processed we will not call our handler that means we will not involve our command or event handler again
            else await _handler.HandleAsync(command);
        }
    }
}