using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Homiebot.Web
{
    public static class TelemetryHelpers
    {
        public static string ActivityString = "Homiebot";
        public static readonly ActivitySource ActivitySource = new ActivitySource(ActivityString);
        public static Activity? StartActivity(string name, ActivityKind activityKind = ActivityKind.Internal) => Activity.Current is null ? ActivitySource.StartActivity(name, activityKind) : ActivitySource.StartActivity(name, activityKind, Activity.Current?.Id);
    }
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(
                    ac=> {
                        foreach(string jsonfile in Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Configs"),"*.json"))
                        {
                            ac.AddJsonFile(jsonfile,false,true);
                        }
                        ac.Build();
                    }
                )
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
