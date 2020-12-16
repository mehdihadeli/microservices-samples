using System.Runtime.CompilerServices;
using MicroBootstrap.Commands;
using MicroBootstrap.Events;
using MicroBootstrap.Queries;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("Pacco.Services.Availability.Tests.Unit")]
namespace Pacco.Services.Availability.Application
{
    public static class Extensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection serviceCollection)
            => serviceCollection
                .AddCommandHandlers()
                .AddInMemoryCommandDispatcher()

                // register our event handlers
                .AddEventHandlers()
                .AddInMemoryEventDispatcher(); //it is in-memory and sync and don't use rabbitmq
    }
}