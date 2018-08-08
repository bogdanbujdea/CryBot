using Bittrex.Net;
using Bittrex.Net.Interfaces;

using CryBot.Core.Hubs;
using CryBot.Core.Models;
using CryBot.Core.Services;
using CryBot.Web.Infrastructure;

using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.SpaServices.Webpack;
using Microsoft.Extensions.DependencyInjection;

using IHostingEnvironment = Microsoft.Extensions.Hosting.IHostingEnvironment;

using Orleans;
using Orleans.Hosting;
using Orleans.Configuration;

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CryBot.Core.Models.Grains;

namespace CryBot.Web
{
    public class Startup
    {
        private static ISiloHost _siloHost;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<EnvironmentConfig>(Configuration);
            services.AddSingleton<IHostedService, CryptoHostedService>();
            if (Configuration["testMode"] == "False")
                services.AddSingleton(typeof(ICryptoApi), typeof(BittrexApi));
            else
                services.AddSingleton(typeof(ICryptoApi), typeof(FakeBittrexApi));
            services.AddSingleton(typeof(IBittrexClient), typeof(BittrexClient));
            services.AddSingleton(typeof(ITradersManager), typeof(TradersManager));
            services.AddSingleton(typeof(IHubNotifier), typeof(HubNotifier));
            services.AddSingleton(typeof(IPushManager), typeof(PushManager));
            services.AddSingleton(typeof(ISubscriptionsRepository), typeof(SubscriptionsRepository));

            var orleansClient = CreateOrleansClient();
            services.AddSingleton(orleansClient);
            services.AddCors(o => o.AddPolicy("MyPolicy", builder =>
            {
                builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            }));
            services.AddMvc();
            services.AddSignalR();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory)
        {
            try
            {
                var fileName = $"../../Logs/bot-trader-{DateTime.UtcNow:yy-MMM-dd ddd h-mm-ss}.txt";
                loggerFactory.AddFile(fileName);
            }
            catch (Exception)
            {

            }

            applicationLifetime.ApplicationStopping.Register(async () => await _siloHost.StopAsync());
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebpackDevMiddleware(new WebpackDevMiddlewareOptions
                {
                    HotModuleReplacement = true
                });
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseCors("MyPolicy");
            app.UseStaticFiles();
            app.UseSignalR(routes =>
            {
                routes.MapHub<ApplicationHub>("/app");
            });
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");

                routes.MapSpaFallbackRoute(
                    name: "spa-fallback",
                    defaults: new { controller = "Home", action = "Index" });
            });
        }

        private async Task StartSilo()
        {
            var siloBuilder = new SiloHostBuilder()
                .UseLocalhostClustering()
                .UseDashboard(options => { })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "OrleansService";
                })
                .ConfigureEndpoints(IPAddress.Loopback, 11111, 30000, listenOnAnyHostAddress: true)
                .ConfigureLogging(logging => logging.AddConsole())
                .ConfigureApplicationParts(manager =>
                {
                    manager?.AddApplicationPart(typeof(CoinTrader).Assembly).WithReferences();
                });
            var invariant = "System.Data.SqlClient"; // for Microsoft SQL Server
            var connectionString = Configuration["connectionString"].ToString();
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

            _siloHost = siloBuilder.Build();
            await _siloHost.StartAsync();
        }


        private IClusterClient CreateOrleansClient()
        {
            while (true) // keep trying to connect until silo is available
            {
                try
                {
                    StartSilo().Wait();
                    var clientBuilder = new ClientBuilder()
                        //.UseStaticClustering(new IPEndPoint(ipAddress, 30000))
                        .UseLocalhostClustering()
                        .Configure<ClusterOptions>(options =>
                        {
                            options.ClusterId = "dev";
                            options.ServiceId = "OrleansService";
                        })
                        .ConfigureLogging(logging => logging.AddConsole())
                        .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(TraderGrain).Assembly).WithReferences());


                    var client = clientBuilder.Build();
                    client.Connect().Wait();

                    return client;
                }
                catch (Exception e)
                {
                    Thread.Sleep(3000);
                    Console.WriteLine(e);
                }
            }
        }

    }
}
