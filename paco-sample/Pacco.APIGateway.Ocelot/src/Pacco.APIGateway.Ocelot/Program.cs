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
    //in api gateway with synchronous approach the request process sync so api gateway redirect synchronously and wait for response and return response to end user but in api gateway with asynchronous approach once user send request to gateway, it decide send the request to
    // not the service but sending to message broker for example create CreateOrder Command and end user only see 202 accepted and now we accepted the request and now and it is not our concern what comes to to api gateway and what will happen to this command or message
    //because message broker take it and maybe put it in queue and maybe some service subscribe to this message and process this message so we need some way some how get this command process it and return the response to user and the issue here is that there is a lot of
    //messages that exchange in whole of system we need some how correlate end user with operation. we couldn't rely on messageId because in simple scenario we might have one unique message for CreateOrder and another unique messageId for OrderCreated and for each message 
    //that sends to broker and we couldn't corelate them. so we need some sort of common identifier that once we send this message here we get a correlationId and this correlationId should be pass along with each message that is part of this flow whether for example one
    //command or event or chain of commands and events. 

    // rather than synchronouse calls we don't get exceptions in async apis, because after send message to broker with getting a ack from rabbitmq we back and continue to our process and we don't wait for a response and also from publisher
    // perspective when message put in the exchange job done and it note aware about receivers

    //https://www.stevejgordon.co.uk/asp-net-core-correlation-ids

    //CorrelationID is a unique identifier but insted of creating correlationId for each message we will create only once on Api gateway level and then each microservice will be responsible to pass further the same correlationId for message flow. this 
    //is immutable and we create it at entry point our system and it will flow the next each new message take existing correlationId and put in in the next message that we publish within same chain so we can correlate operation 

    //correlationId is a builtin property in rabbitmq like timestap and messageId of rabbitmq that are builtin in the protocol. but we can extend our correlation, instead of just send correlationId we could also send some metadata this called 'CorrelationContext' object
    //we can use amqp headers and pass some additional info such as UserName or UserIdentifier and whatever we need along with it message which doesn't belong to the message but should be part of it (serialize this object as a part of properties.Header property of rabbitmq).

    //-- informing end-user about operation -- : we request comes to api then api sends this message to message broker such as CreateOrder or AddResource command, the we receive this correlationId or requestId that is a unique identifier that we can always send to the gateway to
    //give status of on going request maybe we have a long time request and get the idea what's going one. this is correlationId that we generated  and immediately we return this and Api gateway returns 202 status or accepted after we published message to the message broker 
   
    //we have another service that called operation service this is some sort of infrastructural service could be a background service which will also subscribe to all of the messages that we want to trap that are being process asychrosuly ans will set internal state or internal
    //database with information about this request so if there is a command like CreateOrder it will say I received a command for this correlationId and maybe we set status of this command to pending or new then the end user ask with a HttpGet basedon correlationId but we can also
    //use websocket grpc and some server push technology and send to user information about on going request and then once we received OrderCreated event or OrderRejected event from order service and operation service subscribe to this same event and based on same correlationId
    //our operation service find this and changes its internal state of on going request

    //command will be sent as a pending state because the command itself is nothing more about user intention and inthis point we don't know about the process is successfull or not and if we will have the full received the integration events (IEvent) with same correlationId
    //we assume the operation succeeded and a RejectedEvent(IRejectedEvent) means operation rejected.

    //we set corrlationId in start point that is our api gateway side and in the service level we need to fetch this correlationId from message and pass it further so when we publish message again the correlationId will be pass further 

    //websocket connection between user and operation service doesn't through api gateway but it can through api gateway it is up to us
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
