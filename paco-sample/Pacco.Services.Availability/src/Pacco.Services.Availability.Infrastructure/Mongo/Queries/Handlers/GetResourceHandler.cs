using System.Threading.Tasks;
using MicroBootstrap.Queries;
using MongoDB.Driver;
using Pacco.Services.Availability.Application.DTO;
using Pacco.Services.Availability.Application.Queries;
using Pacco.Services.Availability.Infrastructure.Mongo.Documents;

namespace Pacco.Services.Availability.Infrastructure.Mongo.Queries.Handlers
{
    // handler not resolve when make it internal
    internal sealed class GetResourceHandler : IQueryHandler<GetResource, ResourceDto>
    {
        // because repository doesn't have searching method we should use other datasource, we can inject mongo connection directly in handler in application layer but it os a abstraction leak
        // because application layer aware of type of database we are using. we have 2 solution for this:

        //1. putting query handler in infrastructure so we can inject data connection there and that is not abstraction leak because we are inside infrastructure.
        //2. putting it in application layer and we need to provide some sort of abstraction on top of database for performing query.

        // if we use mongo as a data store we can put it in Infrastructure/Mongo directory and if we choose to use separate database for reading data we could have another adapter  

        // because we are in infrastructure layer we can inject IMongoDatabase but quite often we don't want to use some abstraction for example we use RawSql queries to access our data fast as possible
        // we don't want this orm or database lib overhead. for example when we EF we can inject DBContext by constructor or maybe we can use Dapper or SQLConnection to run query
        private readonly IMongoDatabase _database;

        public GetResourceHandler(IMongoDatabase database)
        {
            _database = database;
        }

        public async Task<ResourceDto> HandleAsync(GetResource query)
        {
            var document = await _database.GetCollection<ResourceDocument>("resources")
                .Find(r => r.Id == query.ResourceId)
                .SingleOrDefaultAsync();
            
            return document?.AsDto();
        }
    }
}