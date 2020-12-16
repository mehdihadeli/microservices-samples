using System.Threading.Tasks;
using MicroBootstrap.Commands.Dispatchers;
using MicroBootstrap.Events;
using Pacco.Services.Availability.Application.Commands;

namespace Pacco.Services.Availability.Application.Events.External.Handlers
{
    public class VehicleDeletedHandler : IEventHandler<VehicleDeleted>
    {
        private readonly ICommandDispatcher _dispatcher;

        public VehicleDeletedHandler(ICommandDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public Task HandleAsync(VehicleDeleted @event)
        {
           return _dispatcher.SendAsync(new DeleteResource(@event.VehicleId));
        }
    }
}