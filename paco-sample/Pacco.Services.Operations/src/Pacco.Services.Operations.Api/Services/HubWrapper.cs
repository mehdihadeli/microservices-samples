using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Pacco.Services.Operations.Api.Hubs;
using Pacco.Services.Operations.Api.Infrastructure;

namespace Pacco.Services.Operations.Api.Services
{
    public class HubWrapper : IHubWrapper
    {
        private readonly IHubContext<PaccoHub> _hubContext;

        public HubWrapper(IHubContext<PaccoHub> hubContext)
        {
            _hubContext = hubContext;
        }
        
        /// <param name="method">The name of the method to invoke.</param>
        /// <param name="data">argument for this method</param>
        public async Task PublishToUserAsync(string userId, string method, object data)
        {
            if (string.IsNullOrEmpty(userId))
                await PublishToAllAsync(method, data);
            await _hubContext.Clients.Group(userId.ToUserGroup()).SendAsync(method, data);
        }

        /// <param name="method">The name of the method to invoke.</param>
        /// <param name="data">argument for this method</param>
        public async Task PublishToAllAsync(string method, object data)
            => await _hubContext.Clients.All.SendAsync(method, data);
    }
}