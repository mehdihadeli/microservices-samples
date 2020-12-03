using DShop.Common.Logging;
using DShop.Common.Metrics;
using DShop.Common.Vault;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace DShop.Services.Discounts
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            //https://edi.wang/post/2019/1/5/auto-refresh-settings-changes-in-aspnet-core-runtime
            WebHost.CreateDefaultBuilder(args) //application configuration register here

            //vault overrid some of configurations
            .UseVault()
                .UseStartup<Startup>()

                //Seq use for integrated all services and their instance place in a single integrated log system
                .UseLogging() //Seq and SeriLog
                .UseAppMetrics();
    }
}
