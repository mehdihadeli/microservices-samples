using System;

namespace Pacco.Services.Availability.Core.Exceptions
{
    public abstract class DomainException : Exception
    { 
        public virtual string Code { get; } //code use for front-end, we don't want return english description beside of our information

        protected DomainException(string message) : base(message)
        {
        }
    }
}