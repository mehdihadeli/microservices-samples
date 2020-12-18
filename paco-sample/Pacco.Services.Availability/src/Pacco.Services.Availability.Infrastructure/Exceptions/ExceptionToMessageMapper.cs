using System;
using MicroBootstrap.MessageBrokers.RabbitMQ;
using Pacco.Services.Availability.Application.Commands;
using Pacco.Services.Availability.Application.Exceptions;
using Pacco.Services.Availability.Application.IntegrationEvents.Rejected;
using Pacco.Services.Availability.Core.Exceptions;

namespace Pacco.Services.Availability.Infrastructure.Exceptions
{
    // simple middleware for handling exceptions on the rabbitmq level so whenever we violate some domain invariants in BusSubscriber otherwise we doing some retry command or event handler and return Nack if reach retry limit 
    internal sealed class ExceptionToMessageMapper : IExceptionToMessageMapper
    {
        public object Map(Exception exception, object message)
        {
            return exception switch
            {
                CannotExpropriateReservationException ex => new ReserveResourceRejected(ex.ResourceId, ex.Message, ex.Code),
                ResourceAlreadyExistsException ex => new AddResourceRejected(ex.Id, ex.Message, ex.Code),
                ResourceNotFoundException ex => message switch
                {
                    ReserveResource command => new ReserveResourceRejected(command.ResourceId, ex.Message, ex.Code)
                },
                _ => null
            };
        }
    }
}