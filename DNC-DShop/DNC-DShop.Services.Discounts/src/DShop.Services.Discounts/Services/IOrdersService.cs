using System;
using System.Threading.Tasks;
using DShop.Services.Discounts.Dto;
using RestEase;

namespace DShop.Services.Discounts.Services
{
    //copy past from api gateway and perform some needed change on of it
    public interface IOrdersService
    {
        [AllowAnyStatusCode]
        [Get("orders/{id}")]
        Task<OrderDetailsDto> GetAsync([Path] Guid id);
    }
}