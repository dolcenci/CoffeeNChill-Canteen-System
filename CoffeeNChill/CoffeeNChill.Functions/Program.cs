using System;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CoffeeNChill.Functions.Services;

namespace CoffeeNChill.Functions
{
    public class Program
    {
        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices(services =>
                {
                    var rawConn = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

                    // Build a connection string that works for:
                    // - Microsoft.Azure.Cosmos.Table (old SDK): needs UseDevelopmentStorage=true
                    // - Azure.Storage.Files.Shares (new SDK): needs full connection string
                    string tableConn = "UseDevelopmentStorage=true";
                    string shareConn =
                        "DefaultEndpointsProtocol=http;" +
                        "AccountName=devstoreaccount1;" +
                        "AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;" +
                        "BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;" +
                        "QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;" +
                        "TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;";

                    // If a real connection string is provided (e.g., Azure), use it for both
                    if (!string.IsNullOrEmpty(rawConn) && rawConn != "UseDevelopmentStorage=true")
                    {
                        tableConn = rawConn;
                        shareConn = rawConn;
                    }

                    // Register Storage Account (used by MenuService)
                    services.AddSingleton<CloudStorageAccount>(sp =>
                        CloudStorageAccount.Parse(tableConn));

                    // Register Menu Service
                    services.AddScoped<MenuService>(sp =>
                    {
                        var storageAccount = sp.GetRequiredService<CloudStorageAccount>();
                        var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<MenuService>>();
                        return new MenuService(storageAccount, logger);
                    });

                    // Register Document Service (uses the new Azure.Storage SDK)
                    services.AddScoped<DocumentService>(sp =>
                    {
                        var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DocumentService>>();
                        return new DocumentService(shareConn, "staff-docs", logger);
                    });

                    // Register Order Service (Part 2)
                    services.AddScoped<OrderService>(sp =>
                    {
                        var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<OrderService>>();
                        return new OrderService(tableConn, "order-processing-queue", "Orders", logger);
                    });
                })
                .Build();

            host.Run();
        }
    }
}