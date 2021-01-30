using System;
using NBomber.Contracts;
using NBomber.Plugins.Http.CSharp;
using NBomber.Plugins.Network.Ping;
using NBomber.CSharp;
using Xunit;
using Newtonsoft.Json;
using Pacco.Services.Availability.Core.Entities;
using Shouldly;
using System.Linq;
using NBomber.Http;

namespace Pacco.Services.Availability.Tests.Performance
{
    public class PerformanceTests
    {
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
                //.WithDuration(TimeSpan.FromSeconds(10))
                .WithWarmUpDuration(TimeSpan.FromSeconds(duration))
                .WithLoadSimulations(new[]
                {
                    //Simulation.InjectPerSec(rate: 100, during: TimeSpan.FromSeconds(30)),
                    Simulation.KeepConstant(copies: 1, during: TimeSpan.FromSeconds(2))
                });

            var pingPlugin = new PingPlugin(pingPluginConfig);

            // act
            NodeStats nodeStats = NBomberRunner.RegisterScenarios(scenario)
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
    }
}