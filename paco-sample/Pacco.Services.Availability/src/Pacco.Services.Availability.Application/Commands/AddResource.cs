using System;
using System.Collections.Generic;
using System.Linq;
using MicroBootstrap.Commands;

namespace Pacco.Services.Availability.Application.Commands
{
    // command name should be imperatice and what we want to do. and we don't have suffix for our command and query 
    // we need our command to be immutable, we don't want change them.
    // command use for user intention. user want to intract with system and here create this input as a command and we don't want change user intention
    
    [Contract]
    public class AddResource : ICommand
    {
        public Guid ResourceId { get; }
        public IEnumerable<string> Tags { get; }

        public AddResource(Guid resourceId, IEnumerable<string> tags)
            => (ResourceId, Tags) = (resourceId == Guid.Empty ? Guid.NewGuid() : resourceId,
                tags ?? Enumerable.Empty<string>());
    }
}