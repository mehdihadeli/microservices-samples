using System.Threading.Tasks;
using App.Metrics.AspNetCore;
using MicroBootstrap;
using MicroBootstrap.Authentication;
using MicroBootstrap.Jaeger;
using MicroBootstrap.Logging;
using MicroBootstrap.MessageBrokers.RabbitMQ;
using MicroBootstrap.Redis;
using MicroBootstrap.Security;
using MicroBootstrap.Vault;
using MicroBootstrap.WebApi;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;
using Pacco.APIGateway.Ocelot.Infrastructure;

namespace Pacco.APIGateway.Ocelot
{
    // don't get exceptions in async apis, because after send message to broker with getting a ack from rabbitmq we back and continue to our process and we don't wait for a response and also from publisher
    // perspective when message put in the exchange job done and it note aware about receivers
    public class Program
    {
        public static Task Main(string[] args) => CreateHostBuilder(args).Build().RunAsync();

        //web api endpoints which works synchronously that defined in target service and we can call the using settings in ocelot but we also have asynchronous messages with use SubscribeCommand and SubscribeEvent in 
        //infra layer of target service so we able to receive this messages comming from message broker and process them asychrosuly (in our Api gateway we use AsyncRoutingMiddleware to handle this feature) and 
        //we defined our async endpoints in our AsyncRoutes section of ocelot.json
        public static IHostBuilder CreateHostBuilder(string[] args)
            => Host.CreateDefaultBuilder(args) //will load default app settings
                                               //https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-5.0#default-configuration
                                               //https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-5.0#json-configuration-provider
                                               //https://andrewlock.net/creating-a-custom-iconfigurationprovider-in-asp-net-core-to-parse-yaml/
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath)
                        .AddJsonFile("appsettings.json", false)
                        .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", true, true)
                        .AddJsonFile("ocelot.json")  // load ocelot from separate file
                        .AddEnvironmentVariables();
                })
                .ConfigureWebHostDefaults(builder => builder
                    .ConfigureServices(services =>
                    {
                        // services.AddMetrics();
                        services.AddHttpClient();
                        services.AddSingleton<IPayloadBuilder, PayloadBuilder>();
                        services.AddSingleton<ICorrelationContextBuilder, CorrelationContextBuilder>();
                        services.AddSingleton<IAnonymousRouteValidator, AnonymousRouteValidator>();
                        services.AddTransient<AsyncRoutesMiddleware>();
                        services.AddTransient<ResourceIdGeneratorMiddleware>();
                        services.AddOcelot()
                            .AddPolly()
                            .AddDelegatingHandler<CorrelationContextHandler>(true);

                        services
                            .AddErrorHandler<ExceptionToResponseMapper>()
                            .AddJaeger()
                            .AddRedis()
                            .AddJwt()
                            .AddRabbitMQ()
                            .AddSecurity()
                            .AddWebApi()
                            ;

                        using var provider = services.BuildServiceProvider();
                        var configuration = provider.GetService<IConfiguration>();
                        
                        // we defined our async endpoints in our AsyncRoutes section of ocelot.json
                        services.Configure<AsyncRoutesOptions>(configuration.GetSection("AsyncRoutes"));
                        //use to define our endpoints that we don't want to protect by token, it will handle by AnonymousRouteValidator and we enforce our endpoint in ocelot setting to use token with AuthenticationOptions
                        services.Configure<AnonymousRoutesOptions>(configuration.GetSection("AnonymousRoutes"));
                    })
                    .Configure(app =>
                    {
                        app.UseErrorHandler();
                        app.UseAuthentication(); // Must be after UseRouting()
                        app.UseAuthorization(); // Must be after UseAuthentication()
                        app.UseRabbitMQ();
                        app.UseAccessTokenValidator();
                        //https://docs.microsoft.com/en-us/aspnet/core/fundamentals/routing?view=aspnetcore-5.0
                        app.MapWhen(ctx => ctx.Request.Path == "/", a =>
                        {
                            a.Use((ctx, next) =>
                            {
                                var appOptions = ctx.RequestServices.GetRequiredService<AppOptions>();
                                return ctx.Response.WriteAsync(appOptions.Name);
                            });
                        });

                        //we define this two middleware before running ocelot middleware
                        
                        //so if it is async endpoint call, we don't continue for using ocelot middleware with calling next(context) method and we will terminate middleware pipelines here (terminal middleware) 
                        app.UseMiddleware<AsyncRoutesMiddleware>();
                        app.UseMiddleware<ResourceIdGeneratorMiddleware>();

                        app.UseOcelot(GetOcelotConfiguration()).GetAwaiter().GetResult();
                    })
                    .UseLogging()
                    //.UseVault()
                    //.UseMetrics()
                    );

        private static OcelotPipelineConfiguration GetOcelotConfiguration()
            => new OcelotPipelineConfiguration
            {
                AuthenticationMiddleware = async (context, next) =>
                {
                    await next.Invoke();
                    return;
                    // if (!context.DownstreamReRoute.IsAuthenticated)
                    // {
                    //     await next.Invoke();
                    //     return;
                    // }

                    // if (context.HttpContext.RequestServices.GetRequiredService<IAnonymousRouteValidator>()
                    //     .HasAccess(context.HttpContext.Request.Path))
                    // {
                    //     await next.Invoke();
                    //     return;
                    // }

                    // var authenticateResult = await context.HttpContext.AuthenticateAsync();
                    // if (authenticateResult.Succeeded)
                    // {
                    //     context.HttpContext.User = authenticateResult.Principal;
                    //     await next.Invoke();
                    //     return;
                    // }

                    // context.Errors.Add(new UnauthenticatedError("Unauthenticated"));
                }
            };
    }
}
