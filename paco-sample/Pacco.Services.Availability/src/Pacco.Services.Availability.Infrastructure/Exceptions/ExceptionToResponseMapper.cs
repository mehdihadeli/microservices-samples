using System;
using System.Collections.Concurrent;
using System.Net;
using MicroBootstrap;
using MicroBootstrap.WebApi.Exceptions;
using Pacco.Services.Availability.Application.Exceptions;
using Pacco.Services.Availability.Core.Exceptions;

namespace Pacco.Services.Availability.Infrastructure.Exceptions
{
    // simple middleware for handling exceptions on the http level so whenever we hit a handler for the web api and there is ongoing http request
    // and there is an issue and there is an exception we just catch it and map it depending on exception type and return custom error object
    internal sealed class ExceptionToResponseMapper : IExceptionToResponseMapper
    {
        private static readonly ConcurrentDictionary<Type, string> Codes = new ConcurrentDictionary<Type, string>();

        public ExceptionResponse Map(Exception exception)
            => exception switch //fancy switch c# 8
            {
                DomainException ex => new ExceptionResponse(new {code = GetCode(ex), reason = ex.Message},
                    HttpStatusCode.BadRequest), // evaluation of domain invariant we want return BadRequest
                AppException ex => new ExceptionResponse(new {code = GetCode(ex), reason = ex.Message},
                    HttpStatusCode.BadRequest),
                _ => new ExceptionResponse(new {code = "error", reason = "There was an error."},
                    HttpStatusCode.InternalServerError)
            };

        private static string GetCode(Exception exception)
        {
            var type = exception.GetType();
            if (Codes.TryGetValue(type, out var code))
            {
                return code;
            }

            var exceptionCode = exception switch
            {
                DomainException domainException when !string.IsNullOrWhiteSpace(domainException.Code) => domainException
                    .Code,
                AppException appException when !string.IsNullOrWhiteSpace(appException.Code) => appException.Code,
                _ => exception.GetType().Name.ToSnakeCase().Replace("_exception", string.Empty)
            };

            Codes.TryAdd(type, exceptionCode);

            return exceptionCode;
        }
    }
}