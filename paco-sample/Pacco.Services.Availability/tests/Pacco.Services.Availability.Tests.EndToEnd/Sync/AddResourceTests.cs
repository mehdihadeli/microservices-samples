using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Pacco.Services.Availability.Api;
using Pacco.Services.Availability.Application.Commands;
using Pacco.Services.Availability.Infrastructure.Mongo.Documents;
using Pacco.Services.Availability.Tests.Shared.Factories;
using Pacco.Services.Availability.Tests.Shared.Fixtures;
using Shouldly;
using Xunit;
using Xunit.Abstractions;
using System.Net.Http.Json;

namespace Pacco.Services.Availability.Tests.EndToEnd.Sync
{
    public class AddResourceTests : IClassFixture<PaccoApplicationFactory<Program>>,
        IClassFixture<MongoDbFixture<ResourceDocument, Guid>>, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly PaccoApplicationFactory<Program> _factory;
        private readonly MongoDbFixture<ResourceDocument, Guid> _mongoDbFixture;
        private readonly ITestOutputHelper _output;

        public AddResourceTests(PaccoApplicationFactory<Program> factory, MongoDbFixture<ResourceDocument, Guid> mongoDbFixture, ITestOutputHelper output)
        {
            // some part of arrange phase
            _factory = factory;
            factory.Output = output;
            factory.Server.AllowSynchronousIO = true;
            _mongoDbFixture = mongoDbFixture;
            _output = output;
            _mongoDbFixture.CollectionName = "resources";
            _httpClient = factory.CreateClient();
        }
        private Task<HttpResponseMessage> Act(AddResource command) => _httpClient.PostAsJsonAsync("resources", command);

        [Fact]
        public async Task add_resource_endpoint_should_return_http_status_code_created()
        {
            var command = new AddResource(Guid.NewGuid(), new[] { "tag" });

            var response = await Act(command);

            response.ShouldNotBeNull();
            response.StatusCode.ShouldBe(HttpStatusCode.Created);
            response.Headers.Location.ShouldNotBeNull();
            response.Headers.Location.ToString().ShouldBe($"resources/{command.ResourceId}");
        }

        [Fact]
        public async Task add_resource_endpoint_should_return_location_header_with_correct_resource_id()
        {
            var command = new AddResource(Guid.NewGuid(), new[] { "tag" });

            var response = await Act(command);

            var locationHeader = response.Headers.FirstOrDefault(h => h.Key == "Location").Value.First();

            locationHeader.ShouldNotBeNull();
            locationHeader.ShouldBe($"resources/{command.ResourceId}");
        }

        [Fact]
        public async Task add_resource_endpoint_should_add_document_with_given_id_to_database()
        {
            var command = new AddResource(Guid.NewGuid(), new[] { "tag" });

            await Act(command);

            var document = await _mongoDbFixture.GetAsync(command.ResourceId);

            document.ShouldNotBeNull();
            document.Id.ShouldBe(command.ResourceId);
            document.Tags.ShouldBe(command.Tags);
        }



        public void Dispose()
        {
            _mongoDbFixture.Dispose();
        }

        private static StringContent GetContent(object value)
            => new StringContent(JsonConvert.SerializeObject(value), Encoding.UTF8, "application/json");

    }
}