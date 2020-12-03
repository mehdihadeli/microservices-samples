using System;
using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Consul;
using DShop.Common;
using DShop.Common.Consul;
using DShop.Common.Dispatchers;
using DShop.Common.Jaeger;
using DShop.Common.Mongo;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DShop.Common.Mvc;
using DShop.Common.RabbitMq;
using DShop.Common.RestEase;
using DShop.Services.Discounts.Domain;
using DShop.Services.Discounts.Messages.Commands;
using DShop.Services.Discounts.Messages.Events;
using DShop.Services.Discounts.Metrics;
using DShop.Services.Discounts.Repositories;
using DShop.Services.Discounts.Services;
using OpenTracing;

namespace DShop.Services.Discounts
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IContainer Container { get; private set; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddCustomMvc();
            services.AddInitializers(typeof(IMongoDbInitializer));
            services.AddConsul();

            //Distributed Tracing
            services.AddJaeger();
            //we can't add open tracing package to common library because opentracing not support .net standard 2
            //https://github.com/yurishkuro/opentracing-tutorial/tree/master/csharp/src/lesson03
            //https://github.com/opentracing-contrib/csharp-netcore
            //https://andrewlock.net/logging-using-diagnosticsource-in-asp-net-core/
            //https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/DiagnosticSourceUsersGuide.md
            //AddOpenTracing add some span for each request for controller in .net core
            services.AddOpenTracing();
            //based on restease config and property loadbalancer restease use different strategy for handleing request
            services.RegisterServiceForwarder<IOrdersService>("orders-service");
            services.AddTransient<IMetricsRegistry, MetricsRegistry>();

            var builder = new ContainerBuilder();
            //builder.RegisterType<DiscountsRepository>().As<IDiscountsRepository>();
            //bajye inje tak tak register konim kole interface haye assembly ke implement daran ro register mikonim
            builder.RegisterAssemblyTypes(typeof(Startup).Assembly)
                .AsImplementedInterfaces();
            builder.Populate(services);
            //Handling Command and event in-memory
            builder.AddDispatchers();
            builder.AddMongo();
            builder.AddMongoRepository<Customer>("Customers");
            builder.AddMongoRepository<Discount>("Discounts");
            builder.AddRabbitMq();

            Container = builder.Build();

            return new AutofacServiceProvider(Container);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env,
            IApplicationLifetime applicationLifetime, IStartupInitializer initializer,
            IConsulClient consulClient)
        {
            if (env.IsDevelopment() || env.EnvironmentName == "local")
            {
                app.UseDeveloperExceptionPage();
            }

            initializer.InitializeAsync();
            app.UseMvc();

            //work with default routing key for rowrabbit

            //we customize routingkeys
            //*routingkey:discounts.create_discount  exhange:discounts queue:dshop.services.discounts/discounts.create_discount
            //*MessageName is part of routingkey and other part read from namespace confige file that defined in configuration RabbitMQ
            //discounts + CreateDiscount
            //"namespace": "discounts"
            //*event message must be duplicate in different services and use MessageNamespace attribute for crate routingkey correctly
            //custom naming convention in class CustomNamingConventions defined.
            //*default routingkey is namespace + message type and because message live on differnt microservice with diiferent
            //namespace generate different routingkey and dont work 
            //*dispatcher work in-memory like MediateR and don't use event bus that use in discount controller
            //*SubscribeCommand use bus and suitable command get from queue insted of queue
            app.UseRabbitMq()
                //handling error for failed in published message 
                .SubscribeCommand<CreateDiscount>(onError: (cmd, ex) => new CreateDiscountRejected(cmd.CustomerId, ex.Message, "customer_not_found"))
                .SubscribeEvent<CustomerCreated>(@namespace: "customers")
                .SubscribeEvent<OrderCompleted>(@namespace: "orders");
            var serviceId = app.UseConsul();
            //Dispose container when app stopped
            //some times app doesn't shotdown so this line won't work.but it die for this problem we use consul health
            //check-we create ping in home controller to health check
            applicationLifetime.ApplicationStopped.Register(() =>
            {
                //remove specific service instance from consul when app stopped
                consulClient.Agent.ServiceDeregister(serviceId);
                Container.Dispose();
            });
        }
    }
}