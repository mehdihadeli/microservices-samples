using System;
using System.Collections.Generic;
using MicroBootstrap.Types;

namespace Pacco.Services.Availability.Infrastructure.Mongo.Documents
{
    //we don't want store events in resource document
    //Mapping between entity and database
    internal sealed class ResourceDocument : IIdentifiable<Guid>
    {
        public Guid Id { get; set; }
        public int Version { get; set; }
        public IEnumerable<string> Tags { get; set; }
        public IEnumerable<ReservationDocument> Reservations { get; set; }
    }
}