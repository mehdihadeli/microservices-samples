using System;
using System.Threading.Tasks;
using MicroBootstrap.Commands;
using NSubstitute;
using Pacco.Services.Availability.Application.Commands;
using Pacco.Services.Availability.Application.Commands.Handlers;
using Pacco.Services.Availability.Application.DTO.External;
using Pacco.Services.Availability.Application.Exceptions;
using Pacco.Services.Availability.Application.Services;
using Pacco.Services.Availability.Application.Services.Clients;
using Pacco.Services.Availability.Core.Entities;
using Pacco.Services.Availability.Core.Repositories;
using Shouldly;
using Xunit;

namespace Pacco.Services.Availability.Tests.Unit.Application.Handlers
{
    public class ReserveResourceHandlerTests
    {
        private readonly ICommandHandler<ReserveResource> _handler;
        private readonly IResourcesRepository _resourceRepository;
        private readonly IEventProcessor _eventProcessor;
        private readonly ICustomerServiceClient _customersServiceClient;
        private Task Act(ReserveResource command) => _handler.HandleAsync(command);
        public ReserveResourceHandlerTests()
        {
            // Arrange
            _resourceRepository = Substitute.For<IResourcesRepository>();
            _eventProcessor = Substitute.For<IEventProcessor>();
            _customersServiceClient = Substitute.For<ICustomerServiceClient>();
            _handler = new ReserveResourceHandler(_resourceRepository, _eventProcessor, _customersServiceClient);
        }

        [Fact]
        public async Task given_invalid_id_reserve_resource_should_throw_an_exception()
        {
            //Arrange - some arrangements are in constructor
            var command = new ReserveResource(Guid.NewGuid(), DateTime.UtcNow, 0, Guid.NewGuid());

            //Act
            var exception = await Record.ExceptionAsync(async () => await Act(command));

            //Assert
            exception.ShouldNotBeNull();
            exception.ShouldBeOfType<ResourceNotFoundException>();
        }

        [Fact]
        public async Task given_valid_resource_id_for_valid_customer_reserve_resource_should_succeed()
        {
            //Arrange - some arrangements are in constructor
            var command = new ReserveResource(Guid.NewGuid(), DateTime.UtcNow, 0, Guid.NewGuid());
            var resource = Resource.Create(command.CustomerId, new[] { "tag" });
            _resourceRepository.GetAsync(command.ResourceId).Returns(resource); //mock resource

            var customerState = new CustomerStateDto
            {
                State = "valid"
            };
            _customersServiceClient.GetStateAsync(command.CustomerId).Returns(customerState);  //mock customer 

            //Act
            await Act(command);

            //Assert - Verify for calling our needed methods
            await _resourceRepository.Received().UpdateAsync(resource);
            await _eventProcessor.Received().ProcessAsync(resource.Events);
        }
    }
}