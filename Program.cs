using System;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Threading;
using homiebot.voice;
using homiebot.memory;
using Microsoft.EntityFrameworkCore.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Azure.Cosmos;

namespace homiebot
{
    public class HomieBot : BackgroundService
    {
        private readonly ILogger<HomieBot> logger;
        private readonly Random random;
        private readonly IConfiguration configuration;
        private readonly IServiceProvider services;
        public HomieBot(ILogger<HomieBot> logger, Random random, IConfiguration configuration, IServiceProvider services)
        {
            this.logger = logger;
            this.random = random;
            this.configuration = configuration;
            this.services = services;
        }
        
        protected override async Task ExecuteAsync(CancellationToken token)
        {
            logger.LogInformation("Starting Up");
            BotConfig b = configuration.GetSection("BotConfig").Get<BotConfig>();
            DiscordHelper d = new DiscordHelper(b.DiscordToken,b.CommandPrefixes,configuration,logger,services);
            await d.Initialize();
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(1000, token);
            }
            logger.LogInformation("Shutting Down");
            await d.Disconnect();
        }
    }
    class Program
    {
        public static IHostBuilder CreateHostBuilder(string[] args, IConfiguration configuration) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(
                ac=> {
                    foreach(string jsonfile in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory,"*.json"))
                    {
                        ac.AddJsonFile(jsonfile,false,true);
                    }
                    ac.Build();
                })
            .ConfigureLogging( l => {
                l.AddConsole().AddDebug();
            })
            .ConfigureServices((hostContext, services) =>
            {
                var cosmosconf = configuration.GetSection("CosmosStorageConfig").Get<CosmosStorageConfig>();
                services.AddSingleton(typeof(Random))
                .AddSingleton(typeof(ITextToSpeechHelper),typeof(MultiCloudTTS))
                .AddSingleton(typeof(images.IImageStore), typeof(images.AWSS3BucketImageStore))
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
                .AddSingleton(typeof(IMemoryProvider),typeof(EFCoreMemory))
                .AddHostedService<HomieBot>();
            })
            // We can use this to do Windows Service Hosting, but since we're moving this to an appservice...
            .UseWindowsService();

        static async Task Main(string[] args)
        {
            // We need to build the configuration in advance once, there has to be a better way
            var appconfbuilder = new ConfigurationBuilder();
            foreach(string jsonfile in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory,"*.json"))
            {
                appconfbuilder.AddJsonFile(jsonfile,false,true);
            }
            var config = appconfbuilder.Build();
            await CreateHostBuilder(args, config).Build().RunAsync();
            /*
            Log("Homiebot starting up");
            config = JsonSerializer.Deserialize<BotConfig>(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"BotConfig.json")));
            Log("Parsing BotConfig");
            discord = new DiscordHelper(config.DiscordToken);
            Log("Adding exit handler");
            Console.CancelKeyPress += new ConsoleCancelEventHandler(Exit);
            Log("Initializing RNG");
            random = new Random();
            Log("Loading Chat Replacer");
            chatReplacer = new ChatReplacer();
            chatReplacer.Initialize();
            Log("Initializing Discord");
            await discord.Initialize(chatReplacer);
            discord.RegisterCommands();
            Log("Discord Initialized. Bot Running. Ctrl-C to Exit");
            await Task.Delay(-1);
            Log("Exiting");
            return await Exit();
            */
        }    
    }
}
