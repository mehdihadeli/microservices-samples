using System.Threading.Tasks;
using DShop.Common.Handlers;
using DShop.Common.RabbitMq;
using DShop.Common.Types;
using DShop.Services.Discounts.Domain;
using DShop.Services.Discounts.Messages.Commands;
using DShop.Services.Discounts.Messages.Events;
using DShop.Services.Discounts.Repositories;
using Microsoft.Extensions.Logging;

namespace DShop.Services.Discounts.Handlers.Discounts
{
    //Actualy Application Service class

    //most advantage of command pattern and command handler extract each domain
    //to some class to handle something you want. for easier to read and understand
    public class CreateDiscountHandler : ICommandHandler<CreateDiscount>
    {
        private readonly IDiscountsRepository _discountsRepository;
        private readonly ICustomersRepository _customersRepository;
        private readonly IBusPublisher _busPublisher;
        private readonly ILogger<CreateDiscountHandler> _logger;

        public CreateDiscountHandler(IDiscountsRepository discountsRepository,
            ICustomersRepository customersRepository,
            IBusPublisher busPublisher, ILogger<CreateDiscountHandler> logger)
        {
            _discountsRepository = discountsRepository;
            _customersRepository = customersRepository;
            _busPublisher = busPublisher;
            _logger = logger;
        }

        // Command -> Event - how to track this simple use case?
        // Command -> Event -> Event -> Command -> Event ... or this one?
        //ICorrelationContext is metadata comes with message and use in message flow and we will not mutate this
        //ICorrelationContext and we create this object in the begining of inside APIGateway and let it go with the
        //messages and use this ICorrelationContext class in message handlers as a second parameter


        //so in base controller of api gateway with send async we create new CorrelationContext and publish our message
        //and its context to the bus for first time(with buspublisher.sendasync(command,context)) and all of the message 
        //handlers and get this message and context and in order to pass it further we pass it with buspublish sendasync
        //or publishasync method and pass same context from api that was assigned in the first place for this issue who
        //originated the first request  
        public async Task HandleAsync(CreateDiscount command, ICorrelationContext context)
        {
            //we can use httpclient for check customer existing but it call synchronous and maybe service
            //temporary down or not responding to our request and our handler fail but by using this asynchronous 
            //integration with customercreated event and customercreatedhandler we keeping our internal data(share data)
            //we get a lot of resiliency because data is here in our service that actually needs it at this time and ask
            //it from its own database and not ask from external service to get this customer

            // Customer validation
            var customer = await _customersRepository.GetAsync(command.CustomerId);

            //comment null check and so we can null reference exception and run retry number 1 from polly
            if (customer is null)
            {
                //onError: -> publish CreateDiscountRejected
                //run retry number 1 from polly
                throw new DShopException("customer_not_found",
                    $"Customer with id: '{command.CustomerId}' was not found.");

                //     //we can throw an exception to user or return response code or message but we can use event
                //     //we have a happy event discount created and now lets create a event like CreateDiscountFailed
                //     //and specially for this scenario we created an interface called IRejectedInterface that have two
                //     //property reason that we can put exception message and code for our error code
                //     //we create a event CreatedDiscountRejected
                //     _logger.LogWarning($"Customer with id: '{command.CustomerId}' was not found.");

                //      //when publish a command first discount 
                //      await _busPublisher.PublishAsync(new CreateDiscountRejected(command.CustomerId,
                //         $"Customer with id: '{command.CustomerId}' was not found.", "customer_not_found"), context);

                //     return;
            }

            // Unique code validation
            var discount = new Discount(command.Id, command.CustomerId,
                command.Code, command.Percentage);
            await _discountsRepository.AddAsync(discount);
            //Send event - what is that happend
            //we can subscribe on this published event in order service
            //we copy and past same event message structure in order service
            //we don't send very large object to message broker for performance issue


            await _busPublisher.PublishAsync(new DiscountCreated(command.Id,
                command.CustomerId, command.Code, command.Percentage), context);
            
            //we want send notification for example email or sms to user we have some options:
            //1.when we publish discount created event we can handle it in our notification service and send a notification with create a DiscountCreatedHandler
            //but suppose we have a lot of types of notifications for example discount,CustomerCreated,AddressUpdated and we have to for each message for each notification type
            //create a message and our notification service must be aware about different things that happening in different services and react them by sending email
            //2.we create a SendEmailNotificationCommand in Notification Service and send a command for sending email

            //event choreography
            //notfication service has to aware of discount service for handling notification or discount service has to aware of notification service for sending notification command to notification service
            //await _busPublisher.SendAsync(new SendEmailNotification());
        }
    }
}