using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using MicroBootstrap;
using Pacco.Services.Availability.Application.Commands;
using Pacco.Services.Availability.Application.Events;
using Pacco.Services.Availability.Application.Events.External;
using Pacco.Services.Availability.Application.Services;
using Pacco.Services.Availability.Application.Services.Clients;
using Pacco.Services.Availability.Core.Repositories;
using Pacco.Services.Availability.Infrastructure.Contexts;
using Pacco.Services.Availability.Infrastructure.Exceptions;
using Pacco.Services.Availability.Infrastructure.Metrics;
using Pacco.Services.Availability.Infrastructure.Mongo.Documents;
using Pacco.Services.Availability.Infrastructure.Mongo.Repositories;
using Pacco.Services.Availability.Infrastructure.Services;
using Pacco.Services.Availability.Infrastructure.Services.Clients;
using MicroBootstrap.WebApi;
using MicroBootstrap.Consul;
using MicroBootstrap.Fabio;
using MicroBootstrap.Jaeger;
using MicroBootstrap.Mongo;
using MicroBootstrap.Queries;
using MicroBootstrap.Redis;
using MicroBootstrap.Security;
using MicroBootstrap.WebApi.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Pacco.Services.Availability.Infrastructure.Jaeger;
using Pacco.Services.Availability.Infrastructure.Logging;
using CorrelationContext = Pacco.Services.Availability.Infrastructure.Contexts.CorrelationContext;
using MicroBootstrap.Events;
using MicroBootstrap.MessageBrokers;
using MicroBootstrap.MessageBrokers.RabbitMQ;
using MicroBootstrap.Commands;

namespace Pacco.Services.Availability.Infrastructure
{
    public static class Extensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            //    services.AddSingleton<IEventMapper, EventMapper>();
            //    services.AddTransient<IMessageBroker, MessageBroker>();
            services.AddTransient<IResourcesRepository, ResourcesMongoRepository>();
            //    services.AddTransient<ICustomersServiceClient, CustomersServiceClient>();
            //    // services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
            //    services.AddTransient<IAppContextFactory, AppContextFactory>();
            //    services.AddTransient<IEventProcessor, EventProcessor>();
            //    services.AddTransient(ctx => ctx.GetRequiredService<IAppContextFactory>().Create());
            //    // services.AddHostedService<MetricsJob>();
            //    services.AddSingleton<CustomMetricsMiddleware>();
            //    // services.TryDecorate(typeof(ICommandHandler<>), typeof(OutboxCommandHandlerDecorator<>));
            //    // services.TryDecorate(typeof(IEventHandler<>), typeof(OutboxEventHandlerDecorator<>));
            // //    services.Scan(s => s.FromAssemblies(AppDomain.CurrentDomain.GetAssemblies())
            // //         .AddClasses(c => c.AssignableTo(typeof(IDomainEventHandler<>)))
            // //         .AsImplementedInterfaces()
            // //         .WithTransientLifetime());

            return services
                    .AddInitializers()
                    .AddErrorHandler<ExceptionToResponseMapper>()
                    .AddQueryHandlers() //we put query part in infrastructure layer instead of application layer
                    .AddInMemoryQueryDispatcher()
                    // .AddEventHandlers()
                    // .AddInMemoryEventDispatcher()
                    // .AddHttpClient()
                    // .AddConsul()
                    // .AddFabio()
                    // .AddRabbitMq()
                    // // .AddMessageOutbox(o => o.AddMongo())
                    // .AddExceptionToMessageMapper<ExceptionToMessageMapper>()
                    .AddMongo()
                    .AddMongoRepository<ResourceDocument, Guid>("resources")
                    .AddRabbitMQ();
            // .AddRedis()
            // // .AddMetrics()
            // .AddJaeger()
            // .AddJaegerDecorators()
            // .AddHandlersLogging()
            // //.AddWebApiSwaggerDocs()
            // .AddCertificateAuthentication()
            // .AddSecurity();
        }

        public static IApplicationBuilder UseInfrastructure(this IApplicationBuilder app)
        {
            //dry principle should use in monolith app but in microservice it may not a good approach. because we don't want share our messages, contract, dto between teams.
            //because we don't want our thosand of service and it git repositories depend on one shared library and tight couple to it. and thosand developer commiting on single
            //repository because we don't want custom message or contract. we also have versioning problem in microservice an update the package may break some internal handlers
            //and we have to publish our contract package before publish each microservice. have share package for messages means the shape of message is exactly the same everwhere
            //but maybe in some service we don't care about some property and we don't want to get some property of a message. also may we have multiple microservice with different language
            //and we can't share package between them. so for dto and message contract we use copy and past in microservice (just local copy)

            app.UseErrorHandler() // it is a middleware for handling error and use ExceptionToResponseMapper
                .UseInitializers()
                .UseRabbitMQ() // it is not a middleware, just for convention purpose
   
                // what the rabbitmq do in behind the scenes is that we will have this publisher/subscriber model when message gets into queue and we have this connection between our service and particular queue 
                // that bind to th exchange and there is a message waiting for us in the queue and rabbitmq will send us a message to us with a push mechanism and it push message to us and we get this message with our subscription.



                //whether it will get from web api as http post or whether it will get from rabbitmq as the message that pushed to our service asynchronously it doesn't matter

                //presentation layer allow us async call with message broker or call api synchronously. and we can handle both of them in our apps

                .SubscribeCommand<ReserveResource>()//for handling command from rabbitmq side asynchronously and finding command handler for receive message in subscribe, beside of handling it directly from web api in-memory and synchronously
                .SubscribeEvent<CustomerCreated>() //for handling event from rabbitmq side asynchronously and finding event handler for receive message in subscribe, beside of handling it directly from web api in-memory and synchronously

                // we cand handle response from message broker with a callback with Subscribe method manually without SubscribeCommand and SubscribeEvent
                // .Subscribe<CustomerCreated>(async (serviceProvider, @event, obj) => 
                // {
                //     using var scope = serviceProvider.CreateScope();
                //     await scope.ServiceProvider.GetRequiredService<IEventHandler<CustomerCreated>>().HandleAsync(@event);
                // });

            // //.UseSwaggerDocs()
            // .UseJaeger()
            // // .UsePublicContracts<ContractAttribute>()
            // // .UseMetrics()
            // // .UseMiddleware<CustomMetricsMiddleware>()
            // .UseCertificateAuthentication()
            ;

            return app;
        }

        internal static CorrelationContext GetCorrelationContext(this IHttpContextAccessor accessor)
            => accessor.HttpContext?.Request.Headers.TryGetValue("Correlation-Context", out var json) is true
                ? JsonConvert.DeserializeObject<CorrelationContext>(json.FirstOrDefault())
                : null;

        internal static IDictionary<string, object> GetHeadersToForward(this IMessageProperties messageProperties)
        {
            const string sagaHeader = "Saga";
            if (messageProperties?.Headers is null || !messageProperties.Headers.TryGetValue(sagaHeader, out var saga))
            {
                return null;
            }

            return saga is null
                ? null
                : new Dictionary<string, object>
                {
                    [sagaHeader] = saga
                };
        }

        internal static string GetSpanContext(this IMessageProperties messageProperties, string header)
        {
            if (messageProperties is null)
            {
                return string.Empty;
            }

            if (messageProperties.Headers.TryGetValue(header, out var span) && span is byte[] spanBytes)
            {
                return Encoding.UTF8.GetString(spanBytes);
            }

            return string.Empty;
        }
    }
}