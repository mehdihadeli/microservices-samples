using System.Threading.Tasks;
using DShop.Common.Handlers;
using DShop.Common.MailKit;
using DShop.Common.RabbitMq;
using DShop.Services.Notifications.Messages.Events;
using DShop.Services.Notifications.Services;

namespace DShop.Services.Notifications.Handlers
{
    //For handling notification for discountcreated event 
    public class DiscountCreatedHandler : IEventHandler<DiscountCreated>
    {
        private readonly MailKitOptions _options;
        private readonly IMessagesService _messagesService;

        public DiscountCreatedHandler(
            MailKitOptions options,
            IMessagesService messagesService)
        {
            _options = options;
            _messagesService = messagesService;
        }
        public Task HandleAsync(DiscountCreated @event, ICorrelationContext context)
        {
            return Task.CompletedTask;
        }
    }
}