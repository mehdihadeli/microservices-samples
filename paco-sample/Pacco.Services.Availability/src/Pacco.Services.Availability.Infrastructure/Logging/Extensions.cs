using MicroBootstrap.Logging.CQRS;
using Microsoft.Extensions.DependencyInjection;
using Pacco.Services.Availability.Application.Commands;

namespace Pacco.Services.Availability.Infrastructure.Logging
{
    internal static class Extensions
    {
        public static IServiceCollection AddHandlersLogging(this IServiceCollection services)
        {
            var assembly = typeof(AddResource).Assembly;
            
            services.AddSingleton<IMessageToLogTemplateMapper>(new MessageToLogTemplateMapper());
            
            return services
                .AddCommandHandlersLogging(assembly)
                .AddEventHandlersLogging(assembly);
        }
    }
}