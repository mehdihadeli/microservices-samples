[End-to-End Testing ASP.NET Core APIs](https://www.youtube.com/watch?v=WWN-9ahbdIU)

[Integration testing | ASP.NET Core 5 REST API Tutorial 15](https://www.youtube.com/watch?v=7roqteWLw4s)

[End-to-End Integration Testing with NServiceBus](https://jimmybogard.com/end-to-end-integration-testing-with-nservicebus/)

[End-to-End Integration Testing with NServiceBus: How It Works](https://jimmybogard.com/end-to-end-integration-testing-with-nservicebus-how-it-works/)

[End-to-End Tests](https://martinfowler.com/articles/practical-test-pyramid.html#End-to-endTests)

[Integration tests in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-5.0)

[SubcutaneousTest](https://martinfowler.com/bliki/SubcutaneousTest.html)

[Subcutaneous Testing in ASP.NET Core](https://josephwoodward.co.uk/2019/03/subcutaneous-testing-asp-net-core)

for end-to-end test will use microsoft library `Microsoft.AspNetCore.MVC.Testing` and `Microsoft.AspNetCore.TestHost`, we able to run our web api in memory and just send http request to this api and validate responses or checkout data in database and we want application start along with database and another required components.

```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="3.1.11" />
    <PackageReference Include="Microsoft.AspNetCore.TestHost" Version="3.1.11" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="16.4.0" />
    <PackageReference Include="Newtonsoft.Json" Version="12.0.3" />
    <PackageReference Include="Shouldly" Version="3.0.2" />
    <PackageReference Include="xunit" Version="2.4.1" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.4.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Pacco.Services.Availability.Api\Pacco.Services.Availability.Api.csproj" />
    <ProjectReference Include="..\..\src\Pacco.Services.Availability.Infrastructure\Pacco.Services.Availability.Infrastructure.csproj" />
    <ProjectReference Include="..\Pacco.Services.Availability.Tests.Shared\Pacco.Services.Availability.Tests.Shared.csproj" />
  </ItemGroup>
```

this approach give us some benefits

- we will have isolation because the application is not reachable from other place rather than `test scenario`
- because we run whole apis in-memory,this whole bootstrapping takes less more time and it will be faster rather starting application in background

we focus on `synchronous tests` because we will be call `web apis` with this in-memory test host and we are not talking about testing some how like rabbitmq or message broker async command and just purely synchronous API calls.

now how we can create client for this web api, if we look at `Pacco.Services.Availability.Tests.Shared` this is the place we share some classes for all kind of testing, in this shared project we have a `WebApplicationFactory` with name of `PaccoApplicationFactory` this class derived from `WebApplicationFactory` provided by asp.net core and allow us create factory for our application and give use possibility to change some thing during normal startup with some change specific for testing like use `HostService` or use other Environment like our example and here we use environment `tests`, which expect we have this `appsettings.tests.json` setting in our test with setting for test purpose. we can move this test file to our test project but we need to change the [application Content Root](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-5.0#how-the-test-infrastructure-infers-the-app-content-root-path) to use your test directory in test project instead of API directory where the `program` or `startup` classes live. generic parameter for this custom factory is `TEntryPoint` and in our case this could be either `startup` or `program` here we don't have startup so we pass program.

```csharp

public class PaccoApplicationFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint> where TEntryPoint : class
{
    public ITestOutputHelper Output { get; set; }

    //use this if we use IHost and generic host
    protected override IHostBuilder CreateHostBuilder()
    {
        var builder = base.CreateHostBuilder();

        //builder.UseContentRoot(""); //https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-5.0#how-the-test-infrastructure-infers-the-app-content-root-path
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders(); // Remove other loggers
            logging.AddXUnit(Output); // Use the ITestOutputHelper instance
        });

        return builder;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        //Be careful: configuration in ConfigureWebHost will overide configuration in CreateHostBuilder
        // we can use settings that defined on CreateHostBuilder but some on them maybe override in ConfigureWebHost both of them add its configurations to `IHostBuilder`

        builder.UseEnvironment("tests"); //https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-5.0#set-the-environment
        // Don't run IHostedServices when running as a test
        builder.ConfigureTestServices((services) => { services.RemoveAll(typeof(IHostedService)); });

        //The test app's builder.ConfigureServices callback is executed after the app's Startup.ConfigureServices code is executed.
        builder.ConfigureServices(services => { });
    }
}
```

[Basic tests with the default WebApplicationFactory](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-5.0#basic-tests-with-the-default-webapplicationfactory)

[Customize WebApplicationFactory](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-5.0#customize-webapplicationfactory)

[Customize the client with WithWebHostBuilder](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-5.0#customize-the-client-with-withwebhostbuilder)

[Exploring the new project file, Program.cs, and the generic host](https://andrewlock.net/exploring-the-new-project-file-program-and-the-generic-host-in-asp-net-core-3/)

[.NET Generic Host](https://docs.microsoft.com/en-us/dotnet/core/extensions/generic-host)

[Converting integration tests to .NET Core 3.0](https://andrewlock.net/converting-integration-tests-to-net-core-3/)

[Using custom startup class with ASP.NET Core integration tests](https://gunnarpeipman.com/aspnet-core-integration-test-startup/)

[Using custom appsettings.json with ASP.NET Core integration tests](https://gunnarpeipman.com/aspnet-core-integration-tests-appsettings/)

Since we making this web api we can call it `Sync Tests` and we are not talking about async message broker involve testing. lets tart with testing AddResource scenario not only on core level but on the overall application level or web api level and we can call it `AddResourceTest`.
for this tets we need a `ClassFixture` with implement a generic interface `IClassFixture<>` and we can use dependency injection for xunit

[Shared Context between Tests](https://xunit.net/docs/shared-context#class-fixture)

[XUnit – Part 5: Share Test Context With IClassFixture and ICollectionFixture](https://hamidmosalla.com/2020/02/02/xunit-part-5-share-test-context-with-iclassfixture-and-icollectionfixture/)

[Share Expensive Object Between Tests By IClassFixture](https://hamidmosalla.com/2018/07/21/share-expensive-object-between-tests-by-iclassfixture/)

so we use `IClassFixture<PaccoApplicationFactory<Program>>` there Program will use for determine entry point path our application and here API project path.

```csharp
public class AddResourceTests : IClassFixture<PaccoApplicationFactory<Program>>,
        IClassFixture<MongoDbFixture<ResourceDocument, Guid>>, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly PaccoApplicationFactory<Program> _factory;
        private readonly MongoDbFixture<ResourceDocument, Guid> _mongoDbFixture;
        private readonly ITestOutputHelper _output;

        public AddResourceTests(PaccoApplicationFactory<Program> factory, MongoDbFixture<ResourceDocument, Guid> mongoDbFixture, ITestOutputHelper output)
        {
            _factory = factory;
            factory.Output = output;
            factory.Server.AllowSynchronousIO = true; //https://khalidabuhakmeh.com/dotnet-core-3-dot-0-allowsynchronousio-workaround
            _mongoDbFixture = mongoDbFixture;
            _output = output;
            _mongoDbFixture.CollectionName = "resources";
            _httpClient = factory.CreateClient();
        }

        private Task<HttpResponseMessage> Act(AddResource command) => _httpClient.PostAsync("resources", GetContent(command));
    }
```

we inject `PaccoApplicationFactory<Program>` to our constructor and from this factory we can grab our httpclient, now we can start send the request but if we using newton json the anything that require our kestrel to allow [Synchronous-IO](https://khalidabuhakmeh.com/dotnet-core-3-dot-0-allowsynchronousio-workaround) we need to set `factory.Server.AllowSynchronousIO = true` only if we have some Synchronous-IO rather than async. in this example if we use newton json for serialization.

in this tets we actually test this endpoint of our availability api:

```csharp
 .Post<AddResource>("resources",
    afterDispatch: (cmd, ctx) =>
    {
        return ctx.Response.Created($"resources/{cmd.ResourceId}");
    })
```

we have client that allow use to communicate with our application now we can implement `Act` method. in this act method for testing our sut (resources endpoint with CreatedResource Command), we post CreateResource command to our endpoint `resources` so in our Act method we call a Http Post to `resources` endpoint with passing `AddResource` command and get result from whole this request process.

now we create first test scenario and check once we publish or send http-request we receive `created` or `201` http status code also a location header along with, lets call it `add_resource_endpoint_should_return_http_status_code_created` and we verify both header and status code.

```csharp
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
```

during executing this test we can see data will be place in `resource-test-db` database and our newly created resource will be there. now we have separated database because we have different app settings for testing purposes so did not use production database here. it would be nice looking from test perspective `check` whether the data `inside` the database and actually matches data that passed as a command. also would be nice to have this feature of deleting of database once we finish the testing because now I need clear data in database in some how. so we always start with fresh database and pre initialize data for seeder and our test can run in parallel and they will not interfere with each other.

to achieve this we need a class fixture for mongo. what we can do is doing a post request for creating resource and this should create a resource and eventually store this in mongodb and then we can do a get request but result of this testing execution could depend on both `post` and the `get` so if we mess around the get even though post `works perfectly` fine we can still have some issue (because we don't want integration test for get we cant like this `scenario` in acceptance test for test a scenario and perform multiple request like AddResource, GetResource, ReserveResource) so instead we can do is like doing just `one post request` for web api and plug into mongodb completely beside our application, so we directly connect to mongodb and `seek` for data that we are interested in with using a mongodbFixture for doing this directly on mongodb. here we send a post request and then we fetch data from database and see if it is already there. for this purpose we have a shared MongodbFixture.

this mongo fixture beside of its main operation like get and insert has implemented `IDisposable` interface and inside this dispose method we do `drop` the database so this is a way that we could actually do this clean up at the very end of our testing so inside our test scenarios we could implement IDisposable and dispose this mongodb fixture.

lets implement IDisposable in our `AddResourceTests` class also use `MongodbFixture` in our constructor. and inside of implemented Dispose method we will Dispose our `MongodbFixture` for clean up database after run each tests scenarios.

``` csharp
public void Dispose()
{
    _mongoDbFixture.Dispose();
}
```

lets create another test scenario and use of mongodb, lets call it `add_resource_endpoint_should_add_document_with_given_id_to_database` and after create resource try to fetch document directly from database with `_mongoDbFixture.GetAsync(command.ResourceId);`

``` csharp
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
```
let add a break point on test and run again we can see when this test is running  we will be able to see collection inside mongodb but once whole test will be finished database will be down with using our Dispose method. 

if we want to test get endpoint we can do the same thing but the steps are in different order, first we need to use MongodbFixture to insert the data and then we use web api and call our get for our test.

In this point we do have like test for `web api` or (Sync) but we also need to consider the separated test for our handlers called by rabbitmq (Async)