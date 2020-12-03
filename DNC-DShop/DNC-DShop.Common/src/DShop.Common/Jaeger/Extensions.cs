using Jaeger;
using Jaeger.Reporters;
using Jaeger.Samplers;
using Jaeger.Senders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTracing;
using OpenTracing.Util;
using RawRabbit.Instantiation;

namespace DShop.Common.Jaeger
{
    public static class Extensions
    {
        private static bool _initialized;

        public static IServiceCollection AddJaeger(this IServiceCollection services)
        {
            if (_initialized)
            {
                return services;
            }

            _initialized = true;
            var options = GetJaegerOptions(services);

            if (!options.Enabled)
            {
                var defaultTracer = DShopDefaultTracer.Create();
                services.AddSingleton(defaultTracer);
                return services;
            }

            services.AddSingleton<ITracer>(sp =>
            {
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                //https://github.com/jaegertracing/jaeger-client-csharp/blob/master/src/Jaeger/Reporters/README.md
                var reporter = new RemoteReporter
                        .Builder()
                    .WithSender(new UdpSender(options.UdpHost, options.UdpPort, options.MaxPacketSize))
                    .WithLoggerFactory(loggerFactory)
                    .Build();

                //https://github.com/jaegertracing/jaeger-client-csharp/blob/master/src/Jaeger/Samplers/README.md
                var sampler = GetSampler(options);

                //https://github.com/jaegertracing/jaeger-client-csharp
                //Jaeger clients are language specific implementations of the OpenTracing API
                //https://www.jaegertracing.io/docs/1.13/architecture/
                //Create Tracer Implementation with JaegerClientCsharp for OpenTracer ITracer interface
                //actually opentracer is a abstraction
                var tracer = new Tracer
                        .Builder(options.ServiceName)
                    .WithReporter(reporter)
                    .WithSampler(sampler)
                    .Build();

                GlobalTracer.Register(tracer);

                return tracer;
            });

            return services;
        }

        public static IClientBuilder UseJaeger(this IClientBuilder builder, ITracer tracer)
        {
            builder.Register(pipe => pipe
                .Use<JaegerStagedMiddleware>(tracer));
            return builder;
        }

        private static JaegerOptions GetJaegerOptions(IServiceCollection services)
        {
            using (var serviceProvider = services.BuildServiceProvider())
            {
                var configuration = serviceProvider.GetService<IConfiguration>();
                services.Configure<JaegerOptions>(configuration.GetSection("jaeger"));
                return configuration.GetOptions<JaegerOptions>("jaeger");
            }
        }

        private static ISampler GetSampler(JaegerOptions options)
        {
            //sampler use for when we don't want send every thing for tracing to jaeger but imagine we wll have
            //thousand or milion request and that would paintfull to report every request to jaeger.and we need sample this 
            //data 
            switch (options.Sampler)
            {
                //sample some constant number that was less there is picked by jaeger
                case "const": return new ConstSampler(true);
                //max rate per second, max number trace by jaeger per second
                case "rate": return new RateLimitingSampler(options.MaxTracesPerSecond);
                //we can say trace for example 5% of our total requests
                case "probabilistic": return new ProbabilisticSampler(options.SamplingRate);
                default: return new ConstSampler(true);
            }
        }
    }
}