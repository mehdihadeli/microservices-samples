using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DShop.Common.Handlers;
using DShop.Common.Mongo;
using DShop.Services.Discounts.Domain;
using DShop.Services.Discounts.Dto;
using DShop.Services.Discounts.Metrics;
using DShop.Services.Discounts.Queries;

namespace DShop.Services.Discounts.Handlers.Discounts
{
    public class FindDiscountsHandler : IQueryHandler<FindDiscounts, IEnumerable<DiscountDto>>
    {
        private readonly IMongoRepository<Discount> _discountsRepository;
        private readonly IMetricsRegistry _registry;    
        //for query data usually we don't use repository and we directly connect to database for query data
        //becurse repository contain whole write model and we don't want use this write models for increse performacne
        //for our read model or we can use our dbcontext and create projection for our read model or use dapper.
        //in this sample use repository but for real project that used direrent Read Model we don't use repository

        //if we use direct connection is better use a wrapper for it because if change persistance mechanism we get
        //little change in our code Like IDataService or IFetcherService or IDataSource(create an abstraction for it) and
        //better for mock and unit test
        public FindDiscountsHandler(IMongoRepository<Discount>  discountsRepository, IMetricsRegistry registry)
        {
            _discountsRepository = discountsRepository;
            _registry = registry;
        }

        public async Task<IEnumerable<DiscountDto>> HandleAsync(FindDiscounts query)
        {
            _registry.IncrementFindDiscountsQuery();
            
            var discounts = await _discountsRepository.FindAsync(
                c => c.CustomerId == query.CustomerId);

            return discounts.Select(d => new DiscountDto
            {
                Id = d.Id,
                CustomerId = d.CustomerId,
                Code = d.Code,
                Percentage = d.Percentage,
                Available = !d.UsedAt.HasValue
            });
        }
    }
}