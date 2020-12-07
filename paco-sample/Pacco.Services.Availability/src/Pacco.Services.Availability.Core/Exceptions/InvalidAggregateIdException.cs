using System;

namespace Pacco.Services.Availability.Core.Exceptions
{
    public class InvalidAggregateIdException : DomainException  //for naming convention we can use exception suffix or not
    {
        public override string Code { get; } = "invalid_aggregate_id"; //code use for front-end, we don't want return english description beside of our information
        public Guid Id { get; } // for additional information

        public InvalidAggregateIdException(Guid id) : base($"Invalid aggregate id: {id}")
            => Id = id;
    }
}