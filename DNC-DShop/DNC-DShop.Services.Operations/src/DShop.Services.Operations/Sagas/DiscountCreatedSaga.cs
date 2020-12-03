using System.Threading.Tasks;
using Chronicle;
using DShop.Common.RabbitMq;
using DShop.Services.Operations.Messages.Discounts.Events;
using DShop.Services.Operations.Messages.Notifications.Commands;

namespace DShop.Services.Operations.Sagas
{
    //We can Use MassTransiant or Chronicle for handling Saga
    //we put Saga service in operation service because operation service subscribe to all messages that we pretty much care about so this is a natural place for us
    //to keep Sagas because we can simply subscribe to message that we care about so in this case that will be somthing like discount created event and i will simply create a saga
    // responsible for processing this but we can merge the operations with SignaR we can process operation update signalr and publish data directly without sending operation to bus
    //and handling signalr and sending data for web sockets. we can merge operation and signalr and create another service for Saga

    //in saga base class we have two method complete and reject, Once you call this reject method in our code or some exception thrown saga it self automatically start compensating messages
    //starting from newst to oldest so we want compensate from end to begining and inside in compensate method we have access to particular message

    //order,discount,notification service ,discount service has no idea about notification service and the other way around and for send notification work we use saga
    public class DiscountCreatedSaga : Saga,
        ISagaStartAction<DiscountCreated>//for how our saga start,we start saga when DiscountCreated happend
        //,ISagaAction<EmailSent>, // some participate service in our global transaction
        // ISagaAction<SendEmailNotificationRejected>
    {
        private readonly IBusPublisher _busPublisher;

        public DiscountCreatedSaga(IBusPublisher busPublisher)
        {
            _busPublisher = busPublisher;
        }
        //is for successfull event 
        public Task HandleAsync(DiscountCreated message, ISagaContext context)
        {
            return _busPublisher.SendAsync(new SendEmailNotification("user1@dshop.com",
                    "Discount", $"New discount: {message.Code}"),
                CorrelationContext.Empty);
        }
        //for global rollback, if somthing go wrong 
        public Task CompensateAsync(DiscountCreated message, ISagaContext context)
        {
            //Send an email: discount no longer available
            return Task.CompletedTask;
        }

        // public Task CompensateAsync(EmailSent message, ISagaContext context)
        // {
        //     return Task.CompletedTask;
        // }
        // public Task HandleAsync(EmailSent message, ISagaContext context)
        // {
        //     return Task.CompletedTask;
        // }
        // public Task HandleAsync(SendEmailNotificationRejected message, ISagaContext context)
        // {
        //     //in handle action we aware somthing wrong with sending an email and event we compensate our participate 
        //     //services for rollback them and we start compensate action here
        //     //Global Rollback
        //     CompensateAsync();
        // }

    }

    public class State
    {
        public string Code { get; set; }
    }
}