using System.Net.Http;
using System.Threading.Tasks;
using DShop.Common.Consul;
using DShop.Common.Handlers;
using DShop.Common.RabbitMq;
using DShop.Services.Discounts.Dto;
using DShop.Services.Discounts.Messages.Events;
using DShop.Services.Discounts.Services;
using Newtonsoft.Json;

namespace DShop.Services.Discounts.Handlers.Orders
{
    public class OrderCompletedHandler : IEventHandler<OrderCompleted>
    {
        //restease interface comunication for order service
        private readonly IOrdersService _ordersService;
        private readonly ConsulHttpClient _consulHttpClient;

        public OrderCompletedHandler(IOrdersService ordersService, ConsulHttpClient consulClient)
        {
            this._consulHttpClient = consulClient;
            _ordersService = ordersService;
        }
        //https://foreverframe.net/service-discovery-i-load-balancing-z-consul-i-fabio/
        public async Task HandleAsync(OrderCompleted @event, ICorrelationContext context)
        {
            //we don't use RabbitMQ for get whole date and insted we synchronously ask for this data and we do that with
            //internal apis because api gateway is for external users. 

            //We could use RestEase or HttpClient but we don't use of it.but we don't use of it because configuration
            //and their maintenance for large number of service is hard. 

            // Hard-coded way
            // Level 0
            //Worst way because in order to change the address we have to recompile our application
            //var response = await _httpClient.GetAsync($"http://localhost:5005/orders/{@event.Id}");
            //var content = await response.Content.ReadAsStringAsync();
            //var orderDto = JsonConvert.DeserializeObject<OrderDetailsDto>(content);

            //extract settings to app settings and we can use it without recompiling our application
            // Level 1 - store microservice URL in appsettings.json e.g. for RestEase
            //net enough because we want register our service some where with  registration so we introduce consul as
            //service discovery and consul provide for services self registering and with consul we can get particular
            //service with given key or grouping key  

            //we can use ConsulServiceDiscoveryMessageHandler and use as a Message Handler for ConsulHttpClient with register
            //on it
            //  services.AddHttpClient<ConsulHttpClient>()
            //  .AddHttpMessageHandler<ConsulServiceDiscoveryMessageHandler>();

            //automatic service registration and we use consul
            //Each time the application or kernel resolves that DNS entry, it will receive a randomized round-robin response 
            //of a list of IP addresses which correspond to healthy services in the cluster
            

            //CSSD Client side service discovery and user perform own load balancing
            // Level 2 - use consul for service discovery
            // var orderDto = await _consulHttpClient
            // .GetAsync<OrderDetailsDto>($"orders-service/orders/{@event.Id}");


            //because consul introduce a simple loadbalancing we use fabio and fabio create dynamic routing table and
            //fabio provide loadbalcing using either random or round-robbin 


            //SSSD server side service discovery and using fabio for load balancing
            //we active fabio on order service with set fabio enabled true
            // Level 3 -  additional load balancer - Fabio -Fabio routin table ui is http://localhost:9998
            // var response = await _httpClient.GetAsync($"http://localhost:9999/orders-service/orders/{@event.Id}");

            // Level 4
            //integrate fabio and consul with restease and restease loadbalancer config property
            //to forward request to internal services
            var orderDto = await _ordersService.GetAsync(@event.Id);

            await Task.CompletedTask;
        }
    }
}