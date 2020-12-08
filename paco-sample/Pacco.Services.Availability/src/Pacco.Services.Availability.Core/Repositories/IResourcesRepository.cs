using System.Threading.Tasks;
using Pacco.Services.Availability.Core.Entities;

namespace Pacco.Services.Availability.Core.Repositories
{
    // we create repository for our aggregate to crud operations 

    // if we use full crud generic repository of T we can accidentally do some operation that is not valid
    // for example for order aggregate we shouldn't access to delete order but if we use a generic repository
    // we can do this invalid operation
    public interface IResourcesRepository
    {
        Task<Resource> GetAsync(AggregateId id);
        Task<bool> ExistsAsync(AggregateId id);
        Task AddAsync(Resource resource);
        Task UpdateAsync(Resource resource);
        Task DeleteAsync(AggregateId id);
    }
}