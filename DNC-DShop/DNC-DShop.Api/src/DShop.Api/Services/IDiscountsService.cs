using System;
using System.Threading.Tasks;
using RestEase;

namespace DShop.Api.Services
{
    //API Gateway connect to internal restease and its config for load balancing and choose type of forwarding
    //request to internal services
    [SerializationMethods(Query = QuerySerializationMethod.Serialized)]
    public interface IDiscountsService
    {
        [AllowAnyStatusCode]
        [Get("discounts/{id}")]
        Task<object> GetAsync([Path] Guid id);

        [AllowAnyStatusCode]
        [Get("discounts")]
        Task<object> FindAsync(Guid customerId);
    }
}