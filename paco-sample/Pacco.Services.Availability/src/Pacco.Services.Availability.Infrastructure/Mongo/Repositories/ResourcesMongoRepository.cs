using System;
using System.Threading.Tasks;
using MicroBootstrap.Mongo;
using MongoDB.Driver;
using Pacco.Services.Availability.Core.Entities;
using Pacco.Services.Availability.Core.Repositories;
using Pacco.Services.Availability.Infrastructure.Mongo.Documents;

namespace Pacco.Services.Availability.Infrastructure.Mongo.Repositories
{
    // we use mongo rather sql specially when we use DDD because it is NoSQL and this is based on aggregate and because of aggregate oriented of NoSQL

    // we have four type of NoSQL db: 1) document database like mongo 2) key value database like redis 3) family column like cassandra 4) graph database like new4j
    // and they are aggregate oriented and we keep data in database as a aggregate as a one unit also 

    // what is wrong when we use sql and we use two tables (resource and reservation) and we use lazy loading and we want to fetch the resource that is root and in mean time someone update 
    // the state of this with execute command and once we enumerate on the reservation and we do lazy load and we some data that already updated so my aggregate would be inconsistent or we have different state ask previously 
    // so when we talk about aggregate we don't want use lazy loading we want to load whole aggregate or eager loading 
    internal sealed class ResourcesMongoRepository : IResourcesRepository
    {
        private readonly IMongoRepository<ResourceDocument, Guid> _repository;

        //insted of using inherited Generic repository we use composition, Composition over inheritance. because inherit from generic repository limited us and enforce us to use somemethod that we don't need

        //we could use resource as model but we want to create separate model for use serialize and map to database  also we are limited to the capability of driver for private setter, constructor, virtual collection ,... and
        //we don't want to our domain related tp our orm (seperation of concern - for example orm attributes) and we have to public setter for domain and driver force us so we create separate model 

        //we don't want store events in resource document
        public ResourcesMongoRepository(IMongoRepository<ResourceDocument, Guid> repository)
            => _repository = repository;

        public async Task<Resource> GetAsync(AggregateId id)
        {
            var document = await _repository.GetAsync(r => r.Id == id);
            return document?.AsEntity();
        }

        public Task<bool> ExistsAsync(AggregateId id)
            => _repository.ExistsAsync(r => r.Id == id);

        public Task AddAsync(Resource resource)
            => _repository.AddAsync(resource.AsDocument());

        //version here use for optimistic locking, when we have 2 request concurrently and take the same aggregate and some changes differently, we cover this
        //with use version on aggregate if we have (r.Version < resource.Version) equal or higher there is some concurrency and should reject
        public Task UpdateAsync(Resource resource)
            => _repository.UpdateAsync(resource.AsDocument(), r => r.Id == resource.Id && r.Version < resource.Version);

        // public Task UpdateAsync(Resource resource)
        //     => _repository.Collection.ReplaceOneAsync(r => r.Id == resource.Id && r.Version < resource.Version,
        //         resource.AsDocument());

        public Task DeleteAsync(AggregateId id)
            => _repository.DeleteAsync(id);
    }
}