using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Homiebot.Brain;
using Homiebot.Discord.Voice;
using Homiebot.Images;
using Homiebot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Azure.Cosmos;
using FileContextCore;
using Microsoft.ApplicationInsights.AspNetCore;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.Extensions.Logging;

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
            BotConfig b = Configuration.GetSection("BotConfig").Get<BotConfig>();
            AddImageStore(services,b);
            AddBrains(services,b);
            AddVoice(services,b);
            AddImageProcessor(services,b);
            AddTextAnalyzer(services, b);
            services.AddOpenTelemetry()
                .UseAzureMonitor()
                .WithTracing( t => t.AddSource(TelemetryHelpers.ActivityString));
            services.AddLogging(l => l.AddOpenTelemetry());
            //services.AddApplicationInsightsTelemetry();
            services.AddSingleton(typeof(Random));
            //services.AddSingleton<IJavaScriptSnippet, JavaScriptSnippet>();
            services.AddHostedService<HomieBot>()
                .AddControllersWithViews();
        }

        private void AddImageStore(IServiceCollection services, BotConfig botConfig)
        {
            switch (botConfig.ImageProvider) 
            {
                case nameof(AWSS3BucketImageStore):
                    services.AddSingleton(typeof(IImageStore), typeof(Homiebot.Images.AWSS3BucketImageStore));
                    break;
                case nameof(LocalImageStore):
                    services.AddSingleton(typeof(IImageStore),typeof(Homiebot.Images.LocalImageStore));
                    break;
                default:
                    break;
            }
        }
        private void AddBrains(IServiceCollection services, BotConfig botConfig)
        {
            if(botConfig.UseBrain)
            {
                services.AddDbContext<HomiebotContext>(
                    (options) => {
                        switch (botConfig.BrainProvider)
                        {
                            case "Cosmos":
                                var cosmosconf = Configuration.GetSection("CosmosStorageConfig").Get<CosmosStorageConfig>();                   
                                options.UseCosmos(
                                    cosmosconf.EndPoint,
                                    cosmosconf.ConnectionKey,
                                    cosmosconf.DatabaseName,
                                    options=> {
                                        options.ConnectionMode(ConnectionMode.Gateway);
                                });
                                break;
                            case "LocalFileStorage":
                                var fileConf = Configuration.GetSection("LocalFileStorageConfig").Get<LocalFileStorageConfig>();
                                options.UseFileContextDatabase(databaseName: "homiebot", location: fileConf.StoragePath);
                                break;
                        }
                        

                    }
                );
                services.AddTransient(typeof(IMemoryProvider),typeof(EFCoreMemory));
            }
        }
        private void AddVoice(IServiceCollection services, BotConfig botConfig)
        {
            if(botConfig.UseVoice)
            {
                services.AddSingleton(typeof(ITextToSpeechHelper),typeof(MultiCloudTTS));
            }
            else
            {
                services.AddSingleton(typeof(ITextToSpeechHelper),typeof(DummyTextToSpeechHelper));
            }
        }
        private void AddImageProcessor(IServiceCollection services, BotConfig botConfig)
        {
            services.AddSingleton(typeof(IImageProcessor),
            botConfig.ImageProcessor switch {
                nameof(ImageSharpImageProcessor) => typeof(ImageSharpImageProcessor),
                nameof(MagickSharpImageProcessor) => typeof(MagickSharpImageProcessor),
                nameof(SkiaImageProcessor) => typeof(SkiaImageProcessor),
                _ => throw new NotImplementedException($"Processor {botConfig.ImageProcessor} is not implemented")
            });
        }
        private void AddTextAnalyzer(IServiceCollection services, BotConfig botConfig)
        {
            services.AddSingleton(typeof(ITextAnalyzer),
            botConfig.TextAnalysisProvider switch {
                nameof(AzureTextAnalyzer) => typeof(AzureTextAnalyzer),
                _ => typeof(NoTextAnalyzer)
            }           
            );
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
