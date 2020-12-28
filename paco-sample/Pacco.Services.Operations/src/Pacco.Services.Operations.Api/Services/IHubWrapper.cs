using System.Threading.Tasks;

namespace Pacco.Services.Operations.Api.Services
{
    public interface IHubWrapper
    {
        /// <param name="method">The name of the method to invoke.</param>
        /// <param name="data">argument for this method</param>
        Task PublishToUserAsync(string userId, string method, object data);
        
        /// <param name="method">The name of the method to invoke.</param>
        /// <param name="data">argument for this method</param>
        Task PublishToAllAsync(string method, object data);
    }
}