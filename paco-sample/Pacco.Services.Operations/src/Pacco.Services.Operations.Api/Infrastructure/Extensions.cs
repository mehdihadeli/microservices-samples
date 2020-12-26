using System;
using MicroBootstrap;
using MicroBootstrap.Authentication;
using MicroBootstrap.Commands;
using MicroBootstrap.Consul;
using MicroBootstrap.Events;
using MicroBootstrap.MessageBrokers;
using MicroBootstrap.Queries;
using MicroBootstrap.WebApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Pacco.Services.Operations.Api.Handlers;
using Pacco.Services.Operations.Api.Services;
using Pacco.Services.Operations.Api.Types;
using  MicroBootstrap.Fabio;
using MicroBootstrap.MessageBrokers.RabbitMQ;
using MicroBootstrap.Mongo;
using MicroBootstrap.Redis;
using MicroBootstrap.Jaeger;
using  MicroBootstrap.Security;
using MicroBootstrap.Swagger;

namespace Pacco.Services.Operations.Api.Infrastructure
{
    public static class Extensions
    {
        public static string ToUserGroup(this Guid userId) => userId.ToString("N").ToUserGroup();
        public static string ToUserGroup(this string userId) => $"users:{userId}";

        public static CorrelationContext GetCorrelationContext(this ICorrelationContextAccessor accessor)
        {
            if (accessor.CorrelationContext is null)
            {
                return null;
            }

            var payload = JsonConvert.SerializeObject(accessor.CorrelationContext);

            return string.IsNullOrWhiteSpace(payload)
                ? null
                : JsonConvert.DeserializeObject<CorrelationContext>(payload);
        }

        public static IServiceCollection AddInfrastructure(this IServiceCollection serviceCollection)
        {
            var requestsOptions = serviceCollection.GetOptions<RequestsOptions>("requests");
            serviceCollection.AddSingleton(requestsOptions);
            serviceCollection
                 //add our generic command and event handler for handling operation
                .AddTransient<ICommandHandler<ICommand>, GenericCommandHandler<ICommand>>()
                .AddTransient<IEventHandler<IEvent>, GenericEventHandler<IEvent>>()
                .AddTransient<IEventHandler<IRejectedEvent>, GenericRejectedEventHandler<IRejectedEvent>>()
                .AddTransient<IHubService, HubService>()
                .AddTransient<IHubWrapper, HubWrapper>()
                .AddSingleton<IOperationsService, OperationsService>();
            serviceCollection.AddGrpc();

            return serviceCollection
                .AddErrorHandler<ExceptionToResponseMapper>()
                .AddJwt()
                .AddCommandHandlers()
                .AddEventHandlers()
                .AddQueryHandlers()
                .AddHttpClient()
                .AddConsul()
                .AddFabio()
                .AddRabbitMQ()
                .AddMongo()
                .AddRedis()
                //.AddMetrics()
                .AddJaeger()
                .AddInternalSignalR()
                .AddSecurity();
        }

        public static IApplicationBuilder UseInfrastructure(this IApplicationBuilder app)
        {
            app.UseErrorHandler()
                //.UseSwaggerDocs()
                //.UseJaeger()
                //.UseMetrics()
                .UseStaticFiles()
                .UseRabbitMQ()
                //we want to subscribe to all messages will be process asynchronously or the messages that we are interseted in to be notified when they got completed or rejected for any reason and
                //we notify user through websocket or grpc 
                .SubscribeMessages(); 

            return app;
        }

        private static IServiceCollection AddInternalSignalR(this IServiceCollection serviceCollection)
        {
            var options = serviceCollection.GetOptions<SignalrOptions>("signalR");
            serviceCollection.AddSingleton(options);
            var signalR = serviceCollection.AddSignalR();
            if (!options.Backplane.Equals("redis", StringComparison.InvariantCultureIgnoreCase))
            {
                return serviceCollection;
            }

            var redisOptions = serviceCollection.GetOptions<RedisOptions>("redis");
            signalR.AddRedis(redisOptions.ConnectionString);

            return serviceCollection;
        }
    }
}