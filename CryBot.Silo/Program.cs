using CryBot.Core.Services;

using Microsoft.Extensions.Logging;

using Orleans;
using Orleans.Hosting;
using Orleans.Configuration;

using System;
using System.Net;
using System.Threading.Tasks;

namespace CryBot.Silo
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var siloBuilder = new SiloHostBuilder()
                .UseLocalhostClustering()
                .UseDashboard(options => { })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "OrleansService";                    
                })
                .Configure<EndpointOptions>(options =>                    
                    options.AdvertisedIPAddress = IPAddress.Loopback)
                .ConfigureLogging(logging => logging.AddConsole())
                .ConfigureApplicationParts(manager =>
                    {
                        manager.AddApplicationPart(typeof(CoinTrader).Assembly).WithReferences();
                    });
            var invariant = "System.Data.SqlClient"; // for Microsoft SQL Server
            var connectionString = "Server=tcp:windevcryptodb.database.windows.net,1433;Initial Catalog=cryptodb;Persist Security Info=False;User ID=crypto;Password=CrbogdaN12!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
            siloBuilder.UseAdoNetClustering(options =>
            {
                options.Invariant = invariant;
                options.ConnectionString = connectionString;
            });
            //use AdoNet for reminder service
            siloBuilder.UseAdoNetReminderService(options =>
            {
                options.Invariant = invariant;
                options.ConnectionString = connectionString;
            });
            //use AdoNet for Persistence
            siloBuilder.AddAdoNetGrainStorage("OrleansSqlStore", options =>
            {
                options.Invariant = invariant;
                options.ConnectionString = connectionString;
                options.UseJsonFormat = false;
            });
            using (var host = siloBuilder.Build())
            {                
                await host.StartAsync();
                Console.ReadLine();
            }
        }

    }
}
