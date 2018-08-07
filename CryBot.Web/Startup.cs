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
using Orleans.Configuration;

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace CryBot.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<EnvironmentConfig>(Configuration);
            services.AddSingleton<IHostedService, CryptoHostedService>();
            services.AddSingleton(typeof(ICryptoApi), typeof(FakeBittrexApi));
            services.AddSingleton(typeof(IBittrexClient), typeof(BittrexClient));
            services.AddSingleton(typeof(ITradersManager), typeof(TradersManager));
            services.AddSingleton(typeof(IHubNotifier), typeof(HubNotifier));

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

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
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

        private IClusterClient CreateOrleansClient()
        {
            while (true) // keep trying to connect until silo is available
            {
                try
                {
                    var clientBuilder = new ClientBuilder()
                        .UseStaticClustering(new IPEndPoint(IPAddress.Parse("172.31.197.65"), 30000))
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
                    // log a warning or something
                }
            }
        }

    }
}
