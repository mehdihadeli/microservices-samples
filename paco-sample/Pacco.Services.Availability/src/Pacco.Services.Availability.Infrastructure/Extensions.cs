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
using MicroBootstrap.RabbitMq;
using MicroBootstrap.Redis;
using MicroBootstrap.Security;
using MicroBootstrap.WebApi.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Pacco.Services.Availability.Infrastructure.Jaeger;
using Pacco.Services.Availability.Infrastructure.Logging;
using CorrelationContext = Pacco.Services.Availability.Infrastructure.Contexts.CorrelationContext;

namespace Pacco.Services.Availability.Infrastructure
{
    public static class Extensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
           services.AddSingleton<IEventMapper, EventMapper>();
           services.AddTransient<IMessageBroker, MessageBroker>();
           services.AddTransient<IResourcesRepository, ResourcesMongoRepository>();
           services.AddTransient<ICustomersServiceClient, CustomersServiceClient>();
           // services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
           services.AddTransient<IAppContextFactory, AppContextFactory>();
           services.AddTransient<IEventProcessor, EventProcessor>();
           services.AddTransient(ctx => ctx.GetRequiredService<IAppContextFactory>().Create());
           // services.AddHostedService<MetricsJob>();
           services.AddSingleton<CustomMetricsMiddleware>();
           // services.TryDecorate(typeof(ICommandHandler<>), typeof(OutboxCommandHandlerDecorator<>));
           // services.TryDecorate(typeof(IEventHandler<>), typeof(OutboxEventHandlerDecorator<>));
           services.Scan(s => s.FromAssemblies(AppDomain.CurrentDomain.GetAssemblies())
                .AddClasses(c => c.AssignableTo(typeof(IDomainEventHandler<>)))
                .AsImplementedInterfaces()
                .WithTransientLifetime());

            return services
                .AddErrorHandler<ExceptionToResponseMapper>()
                .AddQueryHandlers()          //we put query part in infrastructure layer instead of application layer
                .AddInMemoryQueryDispatcher()
                .AddHttpClient()
                .AddConsul()
                .AddFabio()
                .AddRabbitMq()
                // .AddMessageOutbox(o => o.AddMongo())
                .AddExceptionToMessageMapper<ExceptionToMessageMapper>()
                .AddMongo()
                .AddMongoRepository<ResourceDocument, Guid>("resources")
                .AddRedis()
                // .AddMetrics()
                .AddJaeger()
                .AddJaegerDecorators()
                .AddHandlersLogging()
                //.AddWebApiSwaggerDocs()
                .AddCertificateAuthentication()
                .AddSecurity();
        }

        public static IApplicationBuilder UseInfrastructure(this IApplicationBuilder app)
        {
            
            app.UseInitializers()
                .UseErrorHandler()
                //.UseSwaggerDocs()
                .UseJaeger()
                // .UsePublicContracts<ContractAttribute>()
                // .UseMetrics()
                // .UseMiddleware<CustomMetricsMiddleware>()
                .UseCertificateAuthentication()
                .UseRabbitMq()
                .SubscribeCommand<AddResource>()
                .SubscribeCommand<DeleteResource>()
                .SubscribeCommand<ReleaseResourceReservation>()
                .SubscribeCommand<ReserveResource>()
                .SubscribeEvent<CustomerCreated>()
                .SubscribeEvent<VehicleDeleted>();

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