using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MicroBootstrap.Events;
using MicroBootstrap.MessageBrokers;
using Microsoft.Extensions.Logging;
using Pacco.Services.Availability.Application.Services;
using MicroBootstrap;
using MicroBootstrap.MessageBrokers.Outbox;

namespace Pacco.Services.Availability.Infrastructure.Services
{
    //IBusSubscriber was registered once we called UseRabbitMQ() inside of our extention we had also a class responsible for publishing message that is IBusPublisher that will register with
    //AddRabbitMq() in extension.

    //IBusPublisher which is a simple abstraction and for this abstraction we have some implentation for rabbitmq, but for some message broker like mass transit we could have different abstraction 
    //so we create our abstraction for using in application layer and we never rely on particular library, we can use IBusPublisher as a abstraction in our Application layer but we don't want to rely on this abstraction
    //in application layer and that's why we make a custom abstraction.

    //we need to register it in our dependency injection manually

    //this is implementation for port that defined in application layer (adapter)
    internal class MessageBroker : IMessageBroker
    {
        private readonly IBusPublisher _busPublisher;
        private readonly ILogger<MessageBroker> _logger;
        private readonly IMessageOutbox _messageOutbox;
        public MessageBroker(IBusPublisher busPublisher, ILogger<MessageBroker> logger, IMessageOutbox messageOutbox)
        {
            this._messageOutbox = messageOutbox;
            this._logger = logger;
            this._busPublisher = busPublisher;

        }
        public async Task PublishAsync(params IEvent[] events)
        {
            await PublishAsync(events.AsEnumerable());
        }

        public async Task PublishAsync(IEnumerable<IEvent> events)
        {
            if (events is null)
            {
                return;
            }

            foreach (var @event in events)
            {
                if (@event is null)
                    continue;

                //we can rely on a library to create a 'unique message event id' for us but we can create a message id with own strategy like guid or Snowflake id

                //this messageId will generate whenever we publish new message and this unique messageId will set in messageId of rabbitmq properties for tracking message in rabbitmq

                //or we generate a unique messageId and publish it with our payload to rabbitmq in web api to track this message that send from web api to rabbitmq, we set this messageId in rammitmq messageId property.

                // will pass to our subscribers. and we use inbox and outbox pattern for handling this messageId
                var messageId = Guid.NewGuid().ToString("N");
                _logger.LogTrace($"Publishing an integration event: '{@event.GetType().Name.ToSnakeCase()}' with ID : '{messageId}'");

                //here we publish our message to rabbitmq but we don't have any receivers, we can create a queue on ui manually and
                //bind this queue with a routing key to a exchange here 'availability'

                // //it publish directly message to message broker but we want to use outbox pattern
                // await _busPublisher.PublishAsync(@event, messageId);

                //instead of sending message directly to broker we send it to outbox and outbox store it in current transaction for inbox and outbox then outbox processor with its background
                //service will publish all unsent messages

                //this part is responsible just for outbox and will guarantee that message will eventually even we have failure, assuming transaction succeeded and processor will start as a background microservice
                //we look for outbox collection and seek for messages that were not sent and it simply publish them
                if (_messageOutbox.Enabled)
                {
                    await _messageOutbox.SendAsync(@event, messageId: messageId);
                    continue;
                }
                await _busPublisher.PublishAsync(@event, messageId: messageId);
            }

        }
    }
}