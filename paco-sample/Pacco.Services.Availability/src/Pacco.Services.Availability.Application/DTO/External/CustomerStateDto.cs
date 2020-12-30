namespace Pacco.Services.Availability.Application.DTO.External
{
    //like local contract for events and commands we like to have local contract for `dtos` (internal point to point communication) and we don't want to have a shared package. 
    //so we create `external directory` in `Dto folder` and we create `CustomerStateDto`.
    public class CustomerStateDto
    {
        public string State { get; set; }
        public bool IsValid => State.Equals("valid", System.StringComparison.InvariantCultureIgnoreCase);
    }
}