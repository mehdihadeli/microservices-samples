using System;

namespace Pacco.Services.Availability.Application.Exceptions
{
    public class ResourceNotFoundException : AppException
    {
        public override string Code { get; } = "resource_not_found"; // can to translate to particular language with globalization in front end
        public Guid Id { get; }

        public ResourceNotFoundException(Guid id) : base($"Resource with id: {id} was not found.")
            => Id = id;
    }
}