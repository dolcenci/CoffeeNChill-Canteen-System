using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CoffeeNChill.Functions.Services;

[assembly: FunctionsStartup(typeof(CoffeeNChill.Functions.Program))]

namespace CoffeeNChill.Functions
{
    public class Program : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Use the connection string from environment or fallback to development
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "UseDevelopmentStorage=true";

            builder.Services.AddSingleton<CloudStorageAccount>(sp =>
                CloudStorageAccount.Parse(connectionString));

            builder.Services.AddScoped<MenuService>(sp =>
            {
                var storageAccount = sp.GetRequiredService<CloudStorageAccount>();
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<MenuService>>();
                return new MenuService(storageAccount, logger);
            });

            builder.Services.AddScoped<DocumentService>(sp =>
            {
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DocumentService>>();
                return new DocumentService(
                    Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "UseDevelopmentStorage=true",
                    "staff-docs",
                    logger);
            });
        }

        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .Build();

            host.Run();
        }
    }
}