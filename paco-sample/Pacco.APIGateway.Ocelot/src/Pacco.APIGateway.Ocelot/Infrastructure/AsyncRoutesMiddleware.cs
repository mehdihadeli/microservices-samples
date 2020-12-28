using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using OpenTracing;
using MicroBootstrap.MessageBrokers;
using MicroBootstrap.MessageBrokers.RabbitMQ;
using MicroBootstrap.MicroBootstrap.MessageBrokers.RabbitMQ.Conventions;

namespace Pacco.APIGateway.Ocelot.Infrastructure
{
    internal sealed class AsyncRoutesMiddleware : IMiddleware
    {
        private readonly IBusPublisher _busPublisher;
        private readonly IPayloadBuilder _payloadBuilder;
        private readonly ITracer _tracer;
        private readonly ICorrelationContextBuilder _correlationContextBuilder;
        private readonly IAnonymousRouteValidator _anonymousRouteValidator;
        private readonly IDictionary<string, AsyncRouteOptions> _routes; // we defined our async endpoints in our AsyncRoutes section of ocelot.json
        private readonly bool _authenticate;

        public AsyncRoutesMiddleware(IBusPublisher busPublisher, IPayloadBuilder payloadBuilder, ITracer tracer,
            ICorrelationContextBuilder correlationContextBuilder, IAnonymousRouteValidator anonymousRouteValidator,
            IOptions<AsyncRoutesOptions> asyncRoutesOptions)
        {
            _busPublisher = busPublisher;
            _payloadBuilder = payloadBuilder;
            _tracer = tracer;
            _correlationContextBuilder = correlationContextBuilder;
            _anonymousRouteValidator = anonymousRouteValidator;
            _routes = asyncRoutesOptions.Value.Routes;
            _authenticate = asyncRoutesOptions.Value.Authenticate == true;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (_routes is null || !_routes.Any())
            {
                await next(context);
                return;
            }

            var key = GetKey(context);
            if (!_routes.TryGetValue(key, out var route)) //read route from ocelot setting with specific key that get from path
            {
                await next(context);
                return;
            }
            //_authenticate read from Authenticate property of AsyncRoutes in ocelot.json
            if ((_authenticate && route.Authenticate != false || route.Authenticate == true) &&
                !_anonymousRouteValidator.HasAccess(context.Request.Path))
            {
                var authenticateResult = await context.AuthenticateAsync();
                if (!authenticateResult.Succeeded)
                {
                    context.Response.StatusCode = 401;
                    return;
                }

                context.User = authenticateResult.Principal;
            }

            var spanContext = _tracer.ActiveSpan is null ? string.Empty : _tracer.ActiveSpan.Context.ToString();
            var message = await _payloadBuilder.BuildFromJsonAsync<dynamic>(context.Request);
            var resourceId = Guid.NewGuid().ToString("N");
            if (context.Request.Method == "POST" && message is JObject jObject)
            {
                jObject.SetResourceId(resourceId);
            }

            // we send a unique messageId with our payload from our web api to rabbitmq to track our message in rabbitmq, we set this messageId in rammitmq messageId property. this messageId in future
            // will pass to our subscribers. and we use inbox and outbox pattern for handling this messageId
            var messageId = Guid.NewGuid().ToString("N");//this is unique per message type, each message has its own messageId in rabbitmq
            var correlationId = Guid.NewGuid().ToString("N");//unique for whole message flow , here gateway initiate our correlationId along side our newly publish message to keep track of our request
            var correlationContext = _correlationContextBuilder.Build(context, correlationId, spanContext,
                route.RoutingKey, resourceId);
            await _busPublisher.PublishAsync<dynamic>(message, messageId, correlationId, spanContext,
                correlationContext, messageConventions: new Conventions(typeof(object), route.RoutingKey, route.Exchange, String.Empty));
            context.Response.StatusCode = 202; //we send 202 status code and a correlationId immediately to end user after we published message to the message broker 
            context.Response.SetOperationHeader(correlationId);
            //so if it is async endpoint call, we don't continue for using ocelot middleware with calling next(context) method and we will terminate middleware pipelines here (terminal middleware) 
        }

        private static string GetKey(HttpContext context) => $"{context.Request.Method} {context.Request.Path}";
    }
}