using System;
using MicroBootstrap.Events;
using MicroBootstrap.Messages;

namespace Pacco.Services.Availability.Application.Events.External
{
    [Message("vehicles")]
    public class VehicleDeleted : IEvent
    {
        public Guid VehicleId { get; }

        public VehicleDeleted(Guid vehicleId)
            => VehicleId = vehicleId;
    }
}