namespace Pacco.Services.Availability.Core.Exceptions
{
    public class InvalidResourceTagsException : DomainException
    {
        public override string Code { get; } = "invalid_resource_tags"; // can to translate to particular language with globalization in front end
        
        public InvalidResourceTagsException() : base("Resource tags are invalid.")
        {
        }
    }
}