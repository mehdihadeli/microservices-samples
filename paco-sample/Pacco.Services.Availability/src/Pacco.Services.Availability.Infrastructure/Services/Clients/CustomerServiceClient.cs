using System;
using System.Net.Http;
using System.Threading.Tasks;
using MicroBootstrap.HTTP;
using Pacco.Services.Availability.Application.DTO.External;
using Pacco.Services.Availability.Application.Services.Clients;

namespace Pacco.Services.Availability.Infrastructure.Services.Clients
{
    //adapter or implementation for ICustomerServiceClient port

    //now we need to implement actual implementation or adapter for this port (ICustomerServiceClient) so we move to Infrastructure layer
    //and Create `Clients` directory and add `CustomerServiceClient`. here we can use `IHttpClientFactory` in our constructor and make a
    //http call and deserialize the response. but here instead of `IHttpClientFactory` we use `IHttpClient` coming from `MicroBootstrap.HTTP`
    //this is some sort of abstraction top of `HttpClient` of .net core.
    internal sealed class CustomerServiceClient : ICustomerServiceClient
    {
        private readonly IHttpClient _httpClient;
        private readonly HttpClientOptions _httpClientOptions;
        private readonly string _url;
        public CustomerServiceClient(IHttpClient httpClient, HttpClientOptions httpClientOptions)
        {
            this._httpClientOptions = httpClientOptions;
            this._httpClient = httpClient;
            this._url = _httpClientOptions.Services["customers"];
        }
        public Task<CustomerStateDto> GetStateAsync(Guid id)
        {
            //here endpoint or query string will be a `constant` value but first part that service actually lives is a `dynamic` value and may change. when we talk about microservices and scale horizontally there maybe new dynamic assigned IP addresses and 
            //new urls and that is very typical in cloud providers so we like some how take this dynamic part and make some one else to responsible for providing this part.
            //this is availability's service `responsibility` to actually be aware where the other microservices `lives` but in real scenario it is not responsibility of availability service and some one else is responsible for this. for the service it should be
            //transparent that service shouldn't care whether it need some load balancing or not it just send a request to some point and expect the response.here for making our httpclient better with using some setting for our httpclient and then we move further and use service discovery.
            return _httpClient.GetAsync<CustomerStateDto>($"{_url}/customers/{id}/state");
        }
    }
}