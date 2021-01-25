using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pacco.Services.Availability.Application.Services;
using Pacco.Services.Availability.Core.Repositories;
using Pacco.Services.Availability.Infrastructure;
using Pacco.Services.Availability.Infrastructure.Mongo.Repositories;
using Pacco.Services.Availability.Infrastructure.Services;
using Xunit.Abstractions;

namespace Pacco.Services.Availability.Tests.Shared.Factories
{
    //https://andrewlock.net/converting-integration-tests-to-net-core-3/
    public class PaccoApplicationFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint> where TEntryPoint : class
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;

        public ITestOutputHelper Output { get; set; }
        public IConfiguration Configuration
        {
            get
            {
                return Services.GetRequiredService<IConfiguration>();
            }
        }
        public IServiceScopeFactory ScopeFactory
        {
            get
            {
                return Services.GetRequiredService<IServiceScopeFactory>();
            }
        }

        // This won't be called because we're using the generic host
        // protected override IWebHostBuilder CreateWebHostBuilder()
        // {
        //     var builder = base.CreateWebHostBuilder();
        //     builder.UseEnvironment("tests");
        //     builder.ConfigureLogging(logging =>
        //     {
        //         logging.ClearProviders();
        //         logging.AddXUnit(Output);
        //     });

        //     return builder;
        // }

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
            builder.ConfigureTestServices((services) =>
            {
                services.RemoveAll(typeof(IHostedService));
            });

            //The test app's builder.ConfigureServices callback is executed after the app's Startup.ConfigureServices code is executed. 
            builder.ConfigureServices(services => { });
        }
    }
}