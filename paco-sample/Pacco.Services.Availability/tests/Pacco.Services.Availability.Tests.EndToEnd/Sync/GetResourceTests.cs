using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Pacco.Services.Availability.Api;
using Pacco.Services.Availability.Application.DTO;
using Pacco.Services.Availability.Infrastructure.Mongo.Documents;
using Pacco.Services.Availability.Tests.Shared.Factories;
using Pacco.Services.Availability.Tests.Shared.Fixtures;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Pacco.Services.Availability.Tests.EndToEnd.Sync
{
    public class GetResourceTests : IClassFixture<PaccoApplicationFactory<Program>>,
        IClassFixture<MongoDbFixture<ResourceDocument, Guid>>, IDisposable
    {
        private Task<HttpResponseMessage> Act() => _httpClient.GetAsync($"resources/{_resourceId}");
        private readonly Guid _resourceId;
        private readonly HttpClient _httpClient;
        private readonly PaccoApplicationFactory<Program> _factory;
        private readonly MongoDbFixture<ResourceDocument, Guid> _mongoDbFixture;
        private readonly ITestOutputHelper _output;

        private Task InsertResourceAsync()
            => _mongoDbFixture.InsertAsync(new ResourceDocument
            {
                Id = _resourceId,
                Tags = new[] { "tag" },
                Reservations = new[]
                {
                    new ReservationDocument
                    {
                        Priority = 0,
                        TimeStamp = DateTime.UtcNow.AsDaysSinceEpoch()
                    }
                }
            });
        public GetResourceTests(PaccoApplicationFactory<Program> factory, MongoDbFixture<ResourceDocument, Guid> mongoDbFixture, ITestOutputHelper output)
        {
            _resourceId = Guid.NewGuid();
            _mongoDbFixture = mongoDbFixture;
            _output = output;
            _factory = factory;
            factory.Output = output;
            _mongoDbFixture.CollectionName = "resources";
            factory.Server.AllowSynchronousIO = true;
            _httpClient = factory.CreateClient();
        }

        [Fact]
        public async Task get_resource_endpoint_should_return_not_found_status_code_if_resource_document_does_not_exist()
        {
            var response = await Act();

            response.ShouldNotBeNull();
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task get_resource_endpoint_should_return_dto_with_correct_data()
        {
            await InsertResourceAsync();
            var response = await Act();

            response.ShouldNotBeNull();
            var content = await response.Content.ReadAsStringAsync();
            var dto = JsonConvert.DeserializeObject<ResourceDto>(content);

            dto.Id.ShouldBe(_resourceId);
        }


        public void Dispose()
        {
            _factory.Output = null;
            _mongoDbFixture.Dispose();
        }

    }
}