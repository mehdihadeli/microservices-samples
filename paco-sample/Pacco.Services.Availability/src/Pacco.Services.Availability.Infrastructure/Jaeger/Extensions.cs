using MicroBootstrap.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Pacco.Services.Availability.Infrastructure.Jaeger
{
    internal static class Extensions
    {
        public static IServiceCollection AddJaegerDecorators(this IServiceCollection services)
        {
            services.TryDecorate(typeof(ICommandHandler<>), typeof(JaegerCommandHandlerDecorator<>));

            return services;
        }
    }
}