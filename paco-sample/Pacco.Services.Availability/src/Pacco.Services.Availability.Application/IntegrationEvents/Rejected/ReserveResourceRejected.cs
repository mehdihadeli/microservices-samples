using System;
using MicroBootstrap.Events;

namespace Pacco.Services.Availability.Application.IntegrationEvents.Rejected
{
    [Contract]
    public class ReserveResourceRejected : IRejectedEvent
    {
        public string Reason { get; }
        public string Code { get; }
        private readonly Guid _resourceId;

        public ReserveResourceRejected(Guid resourceId, string reason, string code)
        {
            this._resourceId = resourceId;
            Reason = reason;
            Code = code;
        }
    }
}