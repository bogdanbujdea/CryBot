using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

using System;

namespace CryBot.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseShutdownTimeout(TimeSpan.FromSeconds(10))
                .UseUrls("http://*:80")
                .Build();
    }
}
