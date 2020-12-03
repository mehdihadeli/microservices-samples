using System.Threading.Tasks;
using DShop.Common.Handlers;
using DShop.Common.RabbitMq;
using DShop.Services.Discounts.Domain;
using DShop.Services.Discounts.Messages.Events;
using DShop.Services.Discounts.Repositories;
using Microsoft.Extensions.Logging;

namespace DShop.Services.Discounts.Handlers.Customers
{
    //share created customer in customer service and subscribe to customer event for insert in our
    //service database as a share data to customer table 
    public class CustomerCreatedHandler : IEventHandler<CustomerCreated>
    {
        private readonly ICustomersRepository _customersRepository;
        private readonly ILogger<CustomerCreatedHandler> _logger;

        public CustomerCreatedHandler(ICustomersRepository customersRepository,
            ILogger<CustomerCreatedHandler> logger)
        {
            _customersRepository = customersRepository;
            _logger = logger;
        }

        public async Task HandleAsync(CustomerCreated @event, ICorrelationContext context)
        {
            await _customersRepository.AddAsync(new Customer(@event.Id, @event.Email));
            _logger.LogInformation($"Created customer with id: '{@event.Id}'.");
        }
    }
}