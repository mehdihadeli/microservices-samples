using System.Collections.Generic;
using System.Threading.Tasks;
using MicroBootstrap;
using MicroBootstrap.Logging;
using MicroBootstrap.Vault;
using MicroBootstrap.WebApi;
using MicroBootstrap.WebApi.CQRS;
using Microsoft.AspNetCore;
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

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
            => WebHost.CreateDefaultBuilder(args)
                .ConfigureServices(services => services
                    .AddWebApi()
                    .AddApplication()
                    .AddInfrastructure())
                .Configure(app => app
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
