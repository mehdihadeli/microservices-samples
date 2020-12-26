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
        // here we use of terminal middlewares, because we want to get ride of internal middlewares for increase performance like NancyFx,
        // but we also have some thing like model binding ourself but if we need performance.

        // controller should be very thin and should be no validation or logic, it should be a simply take the request and process this using some of
        // our component in the application, in cqrs its job is only execute dispatcher

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        => WebHost.CreateDefaultBuilder(args)
            .ConfigureServices(services => services
                .AddWebApi()
                .AddApplication()
                .AddInfrastructure())
            .Configure((IApplicationBuilder app) => app
                .UseInfrastructure()
                // execute same code within controller, either invoke IQueryDispatcher orr ICommandDispatcher for sending command and query
                // if we use get then query dispatcher will be performed 

                //web api endpoints which works synchronously but we also have asynchronous messages with use SubscribeCommand and SubscribeEvent in infra layer so we able to receive this messages comming 
                //from message broker and process them asychrosuly 
                .UseDispatcherEndpoints(endpoints => endpoints
                    .Get("", ctx => ctx.Response.WriteAsync(ctx.RequestServices.GetService<AppOptions>().Name))
                    .Get<GetResources, IEnumerable<ResourceDto>>("resources")
                    .Get<GetResource, ResourceDto>("resources/{resourceId}") // since we want to invoke our dispatcher behind the scenes we can use generic get for pass query (input), output
                    .Post<AddResource>("resources",
                        afterDispatch: (cmd, ctx) =>
                        {
                            return ctx.Response.Created($"resources/{cmd.ResourceId}");
                        })
                    .Post<ReserveResource>("resources/{resourceId}/reservations/{dateTime}") // for post with same id we get 500 internal error but it is actually user abd data and bad request
                    ))
            .UseLogging()
            .UseVault();

        // using controller instead of terminal middlewares
        // public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        // => WebHost.CreateDefaultBuilder(args)
        //     .ConfigureServices(services =>
        //     {
        //         // https://www.jasongaylord.com/blog/2020/07/17/adding-newtonsoft-json-to-services
        //         services.AddControllers().AddNewtonsoftJson(); //add newtonsoft insted of System.Text.Json
        //         services.AddWebApi();
        //         services.AddApplication();
        //         services.AddInfrastructure();
        //     })
        //     .Configure((IApplicationBuilder app) =>
        //     {
        //         app.UseInfrastructure();
        //         app.UseRouting()
        //         .UseEndpoints(e => e.MapControllers());
        //     });
    }
}
