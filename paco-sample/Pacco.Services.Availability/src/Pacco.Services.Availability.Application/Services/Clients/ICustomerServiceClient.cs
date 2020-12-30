using System;
using System.Threading.Tasks;
using Pacco.Services.Availability.Application.DTO.External;

namespace Pacco.Services.Availability.Application.Services.Clients
{
    //this is our port (abstraction)
    public interface ICustomerServiceClient
    {
         Task<CustomerStateDto> GetStateAsync(Guid id);
    }
}