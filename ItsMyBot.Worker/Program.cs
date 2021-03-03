using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;

namespace ItsMyBot.Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
             .ConfigureAppConfiguration((hostContext, builder) =>
             {
                 // Add other providers for JSON, etc.
                 // builder.AddUserSecrets<Program>();
                 
             });

    }
}
