using System;
using Consul;
using DShop.Common.Fabio;
using DShop.Common.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DShop.Common.Consul
{
    public static class Extensions
    {
        private static readonly string ConsulSectionName = "consul";
        private static readonly string FabioSectionName = "fabio";

        public static IServiceCollection AddConsul(this IServiceCollection services)
        {
            IConfiguration configuration;
            using (var serviceProvider = services.BuildServiceProvider())
            {
                configuration = serviceProvider.GetService<IConfiguration>();
            }
            //Register for use in Dependency I njection
            var options = configuration.GetOptions<ConsulOptions>(ConsulSectionName);
            services.Configure<ConsulOptions>(configuration.GetSection(ConsulSectionName));
            services.Configure<FabioOptions>(configuration.GetSection(FabioSectionName));
            services.AddTransient<IConsulServicesRegistry, ConsulServicesRegistry>();
            services.AddTransient<ConsulServiceDiscoveryMessageHandler>();
            services.AddHttpClient<ConsulHttpClient>()
                .AddHttpMessageHandler<ConsulServiceDiscoveryMessageHandler>();

            return services.AddSingleton<IConsulClient>(c => new ConsulClient(cfg =>
            {
                if (!string.IsNullOrEmpty(options.Url))
                {
                    cfg.Address = new Uri(options.Url);
                }
            }));
        }

        //Returns unique service ID used for removing the service from registry.
        public static string UseConsul(this IApplicationBuilder app)
        {
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var consulOptions = scope.ServiceProvider.GetService<IOptions<ConsulOptions>>();
                var fabioOptions = scope.ServiceProvider.GetService<IOptions<FabioOptions>>();
                var enabled = consulOptions.Value.Enabled;
                var consulEnabled = Environment.GetEnvironmentVariable("CONSUL_ENABLED")?.ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(consulEnabled))
                {
                    enabled = consulEnabled == "true" || consulEnabled == "1";
                }

                if (!enabled)
                {
                    return string.Empty;
                }


                var address = consulOptions.Value.Address;
                if (string.IsNullOrWhiteSpace(address))
                {
                    throw new ArgumentException("Consul address can not be empty.",
                        nameof(consulOptions.Value.PingEndpoint));
                }
                //https://cecilphillip.com/using-consul-for-service-discovery-with-asp-net-core/
                // Retrieve Consul client from DI
                var uniqueId = scope.ServiceProvider.GetService<IServiceId>().Id;
                var client = scope.ServiceProvider.GetService<IConsulClient>();
                var serviceName = consulOptions.Value.Service;
                //create unique serviceId per service instance create, init standalone service after creation separately.
                //use when we have multiple instance of a particular microservice and because each microservice has same name
                //we need distinguish between services so for this case and we use serviceId
                var serviceId = $"{serviceName}:{uniqueId}";
                var port = consulOptions.Value.Port;
                var pingEndpoint = consulOptions.Value.PingEndpoint;
                var pingInterval = consulOptions.Value.PingInterval <= 0 ? 5 : consulOptions.Value.PingInterval;
                var removeAfterInterval =
                    consulOptions.Value.RemoveAfterInterval <= 0 ? 10 : consulOptions.Value.RemoveAfterInterval;
                // Register service with consul
                var registration = new AgentServiceRegistration
                {
                    Name = serviceName, //name use for group name a specific service
                    ID = serviceId, //unique name of each instance of a service in a group
                    Address = address,
                    Port = port,
                    //setup fabio tags as SSSD on consul and fabio create a routing table 
                    Tags = fabioOptions.Value.Enabled ? GetFabioTags(serviceName, fabioOptions.Value.Service) : null
                };
                //https://cecilphillip.com/using-consul-for-health-checks-with-asp-net-core/
                //PingEnabled for checking Health
                if (consulOptions.Value.PingEnabled || fabioOptions.Value.Enabled)
                {
                    var scheme = address.StartsWith("http", StringComparison.InvariantCultureIgnoreCase)
                        ? string.Empty
                        : "http://";
                    var check = new AgentServiceCheck
                    {
                        Interval = TimeSpan.FromSeconds(pingInterval),
                        DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(removeAfterInterval),
                        HTTP = $"{scheme}{address}{(port > 0 ? $":{port}" : string.Empty)}/{pingEndpoint}"
                    };
                    registration.Checks = new[] { check };
                }

                client.Agent.ServiceRegister(registration);

                return serviceId;
            }
        }
        //Setup Fabio Tags
        private static string[] GetFabioTags(string consulService, string fabioService)
        {
            var service = (string.IsNullOrWhiteSpace(fabioService) ? consulService : fabioService)
                .ToLowerInvariant();

            return new[] { $"urlprefix-/{service} strip=/{service}" };
        }
    }
}