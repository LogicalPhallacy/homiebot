using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Homiebot;
using Homiebot.Brain;
using Homiebot.Discord;
using Homiebot.Discord.Voice;
using Homiebot.Discord.Commands;
using Homiebot.Images;
using Homiebot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Azure.Cosmos;

namespace Homiebot.Web
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
            var cosmosconf = Configuration.GetSection("CosmosStorageConfig").Get<CosmosStorageConfig>();
            services.AddSingleton(typeof(Random))
                .AddSingleton(typeof(ITextToSpeechHelper),typeof(MultiCloudTTS))
                .AddSingleton(typeof(IImageStore), typeof(Homiebot.Images.AWSS3BucketImageStore))
                .AddDbContext<HomiebotContext>(
                    options => options.UseCosmos(
                        cosmosconf.EndPoint,
                        cosmosconf.ConnectionKey,
                        cosmosconf.DatabaseName,
                        options=> {
                            options.ConnectionMode(ConnectionMode.Gateway);
                        }
                    )
                )
                .AddTransient(typeof(IMemoryProvider),typeof(EFCoreMemory))
                .AddHostedService<HomieBot>()
                .AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}