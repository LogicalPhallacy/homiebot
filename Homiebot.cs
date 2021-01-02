using System;
using System.Threading;
using System.Threading.Tasks;
using Homiebot.Discord;
using Homiebot.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Homiebot 
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
}