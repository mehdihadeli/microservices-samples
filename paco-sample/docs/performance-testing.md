for measure performance of methods or components best way to go to profile this staff is [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet) that is very useful tools for benchmarking component level. 

[Benchmarking C# code using BenchmarkDotNet](https://www.youtube.com/watch?v=EWmufbVF2A4)

[Episode 030 - Analyzing performance with BenchmarkDotNet - ASP.NET Core: From 0 to overkill](https://www.youtube.com/watch?v=8JOC8kN_WbU)

but if we want to measure performance let say `request per second` or `rpc` on top of web api and how does your web api perform in general then we can use some library for measuring performance and there is a lot of tools to choose like [locust](https://github.com/locustio/locust), [gatling](https://gatling.io), [vegeta](https://github.com/tsenart/vegeta), [Netling](https://github.com/hallatore/Netling), [websurge](https://websurge.west-wind.com), [k6](https://k6.io), [NBomber](https://github.com/PragmaticFlow/NBomber). 

but we use a tool that we can use also in .net core which called `NBomber` which is extension of httpclient as well and we create performance test for our web api so if we open our availability we have performance test there and in this project we use this package for NBomber:

``` csharp
<PackageReference Include="NBomber.Http" Version="1.1.1"/>
<PackageReference Include="NBomber" Version="1.1.0"/>
```
[https://github.com/PragmaticFlow/NBomber.Http](https://github.com/PragmaticFlow/NBomber.Http)

[Assertions in docs don't match the way to assert in released nuget package version 0.16](https://github.com/PragmaticFlow/NBomber/issues/190)

[loadtesting-basics](https://nbomber.com/docs/loadtesting-basics)

[Test Assertions](https://nbomber.com/docs/test-automation/#test-assertions)

[HTTP plugin](https://github.com/PragmaticFlow/NBomber/blob/dev/examples/CSharpProd/HttpTests/AdvancedHttpTest.cs)

[load-simulations](https://nbomber.com/docs/core-abstractions#load-simulations)

nice thing about this library is we can either create stand alone `console application` to run tests or we can easily plug it to some testing framework so we have integration with `xunit` so we have performance test running not really as a stand alone app but as a part of our build test framework. maybe we done some pull request but our code is pretty slow and decrease the threshold and number rpc of our system is too low to accept this.

we can easily check what changes when we switch one serializer to another or get ride of mvc controller and stick to router or pure middleware how does it affect overall performance of our api for high preformat system.

here we want to test `get_resources` with http get endpoint and check how many request per second we can get. when we do performance test we need same environment ideally start our microservices in release mode because if we want same comparable result we need a single environment like same cpu, ram.

``` csharp
[Fact]
public void get_resources()
{
    //arrange
    const string url = "http://localhost:5001";
    const string stepName = "init";
    const int duration = 3;
    const int expectedRps = 100;
    var endpoint = $"{url}/resources";

    var step = HttpStep.Create(stepName, context =>
    {
        return Http.CreateRequest("GET", endpoint)
            .WithCheck(async response =>
            {
                var json = await response.Content.ReadAsStringAsync();
                // parse JSON
                var resources = JsonConvert.DeserializeObject<Resource[]>(json);
                return Response.Ok(resources);
            });
    });

    var pingPluginConfig = PingPluginConfig.CreateDefault(new[] { url });

    var scenario = ScenarioBuilder.CreateScenario("GET resources", new[] { step })
        .WithoutWarmUp()
        .WithWarmUpDuration(TimeSpan.FromSeconds(duration))
        .WithLoadSimulations(new[]
        {
            //Simulation.InjectPerSec(rate: 100, during: TimeSpan.FromSeconds(30)),
            Simulation.KeepConstant(copies: 1, during: TimeSpan.FromSeconds(2))
        });

    var pingPlugin = new PingPlugin(pingPluginConfig);

    // act
    var nodeStats = NBomberRunner.RegisterScenarios(scenario)
    .WithWorkerPlugins(pingPlugin)
    .Run();

    //var stepStats = nodeStats.ScenarioStats[0].StepStats[0]; //get info about our first step
     StepStats stepStats = nodeStats.ScenarioStats[0].StepStats.First(q => q.StepName == stepName); // get info about our int step

    // assert
    stepStats.OkCount.ShouldBeGreaterThan(expectedRps * duration);
    stepStats.RPS.ShouldBeGreaterThan(expectedRps);
    // stepStats.Percent75.ShouldBeGreaterThanOrEqualTo(100);
    // stepStats.MinDataKb.ShouldBe(1.0);
    // stepStats.AllDataMB.ShouldBeGreaterThanOrEqualTo(0.01);
}
```

with `.WithWarmUpDuration()` method and duration we specify we want to run test for example for `3 second` and expected result for example we want our web api process at least 100 per second. in post scenario we can use `WithBody()` to provide additional body alongside `WithCheck()` and `WithHeader()` to init our post request but in get request we don't have `WithBody()` and in our response if status code is ok we can go to next `step` in our `scenario`. definition of step is mire like to arrange.

now we need to run our test after that do some assertion. for run one concurrent copy or [some simulation](https://nbomber.com/docs/core-abstractions#load-simulations) we use `Simulation.KeepConstant(copies: 1, during: TimeSpan.FromSeconds(2))` and important part is warmup and same like benchmark .net we can specify whether we want have this warm up meaning that. for example we will run for same 3 second this request so for our api we have proper warmup and here we use `.WithoutWarmup()`. also we use `.WithDuration(3)` for test running for 3 second and then we run our test for our created scenario.

after run our scenario because our scenario could have multiple steps, after get result of running our scenario we get only information about our `init` step . for this step we assert some properties like `RPS` that we want at least 100 rps and also we want successful request that would be `expectedRps * duration` because our test run in 3 second and in each second we should serve at least 100 rps so at least 300 ok responses.  

