using System.Collections.Generic;
using System.Threading.Tasks;
using MicroBootstrap;
using MicroBootstrap.Logging;
using MicroBootstrap.Vault;
using MicroBootstrap.WebApi;
using MicroBootstrap.WebApi.CQRS;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Pacco.Services.Availability.Application;
using Pacco.Services.Availability.Application.Commands;
using Pacco.Services.Availability.Application.DTO;
using Pacco.Services.Availability.Application.Queries;
using Pacco.Services.Availability.Infrastructure;

namespace Pacco.Services.Availability.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
            => await CreateWebHostBuilder(args)
                .Build()
                .RunAsync();

            //https://www.strathweb.com/2020/10/beautiful-and-compact-web-apis-with-c-9-net-5-0-and-asp-net-core/
            //https://www.strathweb.com/2017/01/building-microservices-with-asp-net-core-without-mvc/
            //https://andrewlock.net/converting-a-terminal-middleware-to-endpoint-routing-in-aspnetcore-3/
            //https://docs.microsoft.com/en-us/aspnet/core/fundamentals/routing?view=aspnetcore-5.0#tm
            //https://www.youtube.com/watch?v=pGCHAJnJ1CA
        

            // we get ride of controllers and here startup.cs is not required and we get ride of startup.cs
            public static IWebHostBuilder CreateWebHostBuilder(string[] args)
            => WebHost.CreateDefaultBuilder(args)
                .ConfigureServices(services => services
                    .AddWebApi()
                    .AddApplication()
                    .AddInfrastructure())
                .Configure((IApplicationBuilder app) => app
                    .UseInfrastructure()
                    .UseDispatcherEndpoints(endpoints => endpoints
                        .Get("", ctx => ctx.Response.WriteAsync(ctx.RequestServices.GetService<AppOptions>().Name))
                        .Get<GetResources, IEnumerable<ResourceDto>>("resources")
                        .Get<GetResource, ResourceDto>("resources/{resourceId}")
                        .Post<AddResource>("resources",
                            afterDispatch: (cmd, ctx) => ctx.Response.Created($"resources/{cmd.ResourceId}"))
                        .Post<ReserveResource>("resources/{resourceId}/reservations/{dateTime}")
                        .Delete<ReleaseResourceReservation>("resources/{resourceId}/reservations/{dateTime}")
                        .Delete<DeleteResource>("resources/{resourceId}")))
                .UseLogging()
                .UseVault();
    }
}
