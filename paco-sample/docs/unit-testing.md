core concept about unit testing is that we want to focus to testing particular unit. the definition doesn't precise what we should treat as a particular unit but commonly this like a class or group of classes and key is that we need to provide some sort of `isolation` so we would like to perform in isolated environment so if there is a situation that our command handler depends on other component we like to isolate from them sung other implementing other actual one like custom dummy implementation or some `Mocking` library. because we want to test the behavior of the particular command handler and we would like to sure the execution and failure of this particular test depend on behavior of this handler not the other components.

we create a test project with name of `Pacco.Services.Availability.Tests.Unit` and in project definition we have this references, we also add tow other packages `Shouldly` is a fluent api for assertion and `NSubstitute` for mocking but we can use `Moq` also

``` xml
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="16.5.0" />
    <PackageReference Include="xunit" Version="2.4.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.4.0" />
    <PackageReference Include="coverlet.collector" Version="1.2.0" />
    <PackageReference Include="NSubstitute" Version="4.2.2"/>
    <PackageReference Include="Shouldly" Version="4.0.3"/>
  </ItemGroup>
```

now lets start with our first test and we focus on Core level, we start core because that will be simpler to write also we also see the advantages of having pure function area and because they are not depend on other layers. because we testing core layer we should add a reference to core layer.

with start with create a class called `CreateResourceTests`, naming conventions is up to us and here we don't use pascal case for method name and we use snake case its better when have longer names. test name should be `expressive` and describe whole scenario that we want to test.  

[You are naming your tests wrong!](https://enterprisecraftsmanship.com/posts/you-naming-tests-wrong/)

we have 3 stage in a test, the first one is `arrange` and we prepare whole test environment, this is place for create contract or mock external dependency and then in phase 2 we have `actual scenario to test` and this called `act` and we have `assert` for checking whether the `result` is actually the result that we expect (`expected result`). act is most important one. we create a separate method for `act` with name of `Act` that take `ArrangeId`, `IEnumerable<string>` and inner this method we call `Resource.Create(id, tags)` because this method is our `sut` and method that we want to test (how factory works).

``` csharp
public class CreateResourceTests
{
    private Resource Act(AggregateId id, IEnumerable<string> tags) => Resource.Create(id, tags);
    
    [Fact]
    public void given_valid_id_and_tags_resource_should_be_created()
    {
        // Arrange
        var id  = new AggregateId();
        var tags = new[] {"tag"};

        // Act
        var resource = Act(id,tags);

        // Assert
        resource.ShouldNotBeNull();
        resource.Id.ShouldBe(id);
        resource.Tags.ShouldBe(tags);
        resource.Events.Count().ShouldBe(1); // because we only expect ResourceCreated event added

        var @event = resource.Events.Single();
        @event.ShouldBeOfType<ResourceCreated>(); // verify ResourceCreated event actually exists in events
    }
}
```
now lets make our first test `given_valid_id_and_tags_resource_should_be_created` the method shows what happen and if the test fail our test will fail.

we expect Act method return us a Resource and eventually we need to assert our result.

someone say we should split our test in two separated test first should test the payload or properties of the aggregate and the other check our domain event that could done here but for simplicity we put the in place.

lets create another test for `unhappy path` when we want to create a resource but we hit `MissingResourceTagException`. 

``` csharp
private Resource Act(AggregateId id, IEnumerable<string> tags) => Resource.Create(id, tags);

[Fact]
public void given_empty_tags_resource_should_throw_an_exception()
{
    var id = new AggregateId();
    var tags = Enumerable.Empty<string>();
    
    // Act
    var exception = Record.Exception(() => Act(id, tags));

    // Assert
    exception.ShouldNotBeNull();
    exception.ShouldBeOfType<MissingResourceTagsException>();
}

```

now we have two valid unit test and now we can see having pure domain which has no dependencies to orm, framework, infrastructure we have well encapsulated logic behind some method and it is comfortable to write a test for core layer because the behaviour of particular unit which here is `aggregate` depends only to `inputs`.

now try to test something in application layer and we create another sub directory `Application\Handlers` and we add reference to `application layer` because we want to test it. for our handler they are internal and not visible for our assembly and we need to make the visible here by adding `[assembly: InternalsVisibleTo("Pacco.Services.Availability.Tests.Unit")]` attribute on `Extension` class on top of namespace name `Pacco.Services.Availability.Application`. now application is visible to `Pacco.Services.Availability.Tests.Unit`.

now we want to test `ReserveResourceHandler`, and create a test class and call it `ReserveResourceHandlerTests`

``` csharp
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
}
```
first we define our act with Act method and inner this method we call our handler of type `ReserveResourceHandler`, and this our `sut` that is this method `_handler.HandleAsync(command)` that we want to test. an in xunit for share our arrangement between all tests method we can put them in constructor and it will call before each test runs, in xunit we don't have setup and teardown like NUnit. for teardown we can implement `IDisposable`. our `ReserveResourceHandler` has some dependencies so we need to `mock` them or custom dummy implementation of this class. 

``` csharp
  _resourceRepository = Substitute.For<IResourcesRepository>();
  _eventProcessor = Substitute.For<IEventProcessor>();
  _customersServiceClient = Substitute.For<ICustomerServiceClient>();
  _handler = new ReserveResourceHandler(_resourceRepository, _eventProcessor, _customersServiceClient);
```
now we write first test and name it `given_invalid_id_reserve_resource_should_throw_an_exception`. the first step in our command handler and domain orchestration is fetch resource by ID and checking whether this exists or not we throw an `ResourceNotFoundException`. we try to test potential exception that may happen. 

``` csharp
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
```
the reason of this exception is we did not set `substitute explicitly` for repository and `GetAsync` method, actually we didn't mock this method and return value for each method would be a `default` value and in our case it will be `null` so exception will be thrown.

[https://nsubstitute.github.io/help/creating-a-substitute/](https://nsubstitute.github.io/help/creating-a-substitute/)

now we create another tests valid input that named `given_valid_resource_id_for_valid_customer_reserve_resource_should_succeed`. now in arrange phase beside to provide command we need to create resource that will return by repository, actually we mock our `_resourceRepository.GetAsync()` to return our created `resource`. we also we need to create `CustomerStateDto` and we will mock `_customersServiceClient.GetStateAsync()` to return our created customer state

``` csharp
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
```
after complete arrange phase we need to Act to run our sut in this case `_handler.HandleAsync`. now we need to verify our result and we need to verify `UpdateAsync` was called on `resourceRepository` for our created `resource` and checking `ProcessAsync` was called on `eventProcess`. we don't verify `resource.AddReservation(reservation)` because it is responsibility of `core tests`. this point we check orchestration has been fulfilled.

Received without any parameter means we expect repository and processor receive this call exactly `once`. we can also use `ReceivedWithAnyArgs` or for example we can use predicate if we don't have access to object like `_resourceRepository.Received().UpdateAsync(Arg.Any<Resource>())` or `_resourceRepository.Received().UpdateAsync(Arg.Is<Resource>(r=>r.Id == command.ResourceId))` .

we do have checking this repository and event processor has been called but how we can check whether the events processor eventually publish a message to rabbitmq. we want isolate environment from external dependencies and we should create another unit test for each components that we need and for example we have a `Services` directory and we have another unit test for `EventProcessor` and if event processor has dependency to message broker we need to provide some substitute or some dummy implementation.
If we want to verify whether this event process publish message to broker so need to proceed with integration testing.