using System;
using System.Threading.Tasks;
using MicroBootstrap.Commands;
using Pacco.Services.Availability.Api;
using Pacco.Services.Availability.Application.Commands;
using Pacco.Services.Availability.Application.Commands.Handlers;
using Pacco.Services.Availability.Application.IntegrationEvents;
using Pacco.Services.Availability.Application.Services;
using Pacco.Services.Availability.Core.Repositories;
using Pacco.Services.Availability.Infrastructure.Mongo.Documents;
using Pacco.Services.Availability.Tests.Shared.Factories;
using Pacco.Services.Availability.Tests.Shared.Fixtures;
using Shouldly;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Pacco.Services.Availability.Tests.Integration.Async
{
    public class AddResourceTest2 : IClassFixture<PaccoApplicationFactory<Program>>,
        IClassFixture<MongoDbFixture<ResourceDocument, Guid>>, IDisposable
    {
        private const string Exchange = "availability";
        private readonly MongoDbFixture<ResourceDocument, Guid> _mongoDbFixture;
        private readonly RabbitMqFixture _rabbitMqFixture;
        private readonly ICommandHandler<AddResource> _handler;
        private Task Act(AddResource command) => _handler.HandleAsync(command);
        public AddResourceTest2(PaccoApplicationFactory<Program> factory, MongoDbFixture<ResourceDocument, Guid> mongoDbFixture, ITestOutputHelper output)
        {
            // some part of our arrange phase
            factory.Output = output;
            _rabbitMqFixture = new RabbitMqFixture();
            _mongoDbFixture = mongoDbFixture;
            _mongoDbFixture.CollectionName = "resources";
            factory.Server.AllowSynchronousIO = true;
            using var scope = factory.ScopeFactory.CreateScope();
            var resourceRepository = scope.ServiceProvider.GetService<IResourcesRepository>();
            var eventProcessor = scope.ServiceProvider.GetService<IEventProcessor>();
            var messageBroker = scope.ServiceProvider.GetService<IMessageBroker>();
            _handler = new AddResourceHandler(resourceRepository, messageBroker, eventProcessor);
        }

        [Fact]
        public async Task add_resource_command_should_add_document_with_given_id_to_database()
        {
            var command = new AddResource(Guid.NewGuid(), new[] { "tag" });

            var tcs = _rabbitMqFixture.SubscribeAndGet<ResourceAdded, ResourceDocument>(Exchange, _mongoDbFixture.GetAsync, command.ResourceId);

            await Act(command);

            var document = await tcs.Task;

            document.ShouldNotBeNull();
            document.Id.ShouldBe(command.ResourceId);
            document.Tags.ShouldBe(command.Tags);
        }

        public void Dispose()
        {
            _mongoDbFixture.Dispose();
        }

    }
}