using System;
using MicroBootstrap.Queries;
using Pacco.Services.Availability.Application.DTO;

namespace Pacco.Services.Availability.Application.Queries
{
    // Query name should be imperatice and what we want to do. and we don't have suffix for our command and query 
    // Query could be mutable and immutable, here query isn't immutable and we can add some thing to modify query
    public class GetResource : IQuery<ResourceDto>
    {
        public Guid ResourceId { get; set; }
    }
}