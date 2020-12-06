using System.Runtime.CompilerServices;
using MicroBootstrap.Commands;
using MicroBootstrap.Events;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("Pacco.Services.Availability.Tests.Unit")]
namespace Pacco.Services.Availability.Application
{
    public static class Extensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection serviceCollection)
            => serviceCollection
                .AddCommandHandlers()
                .AddEventHandlers()
                .AddInMemoryCommandDispatcher()
                .AddInMemoryEventDispatcher();
    }
}