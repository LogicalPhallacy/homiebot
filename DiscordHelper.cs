using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace homiebot 
{
    public class DiscordHelper
    {
        private readonly ILogger<HomieBot> logger;
        private readonly IConfiguration config;
        private readonly IServiceProvider services;
        private string token;
        private IEnumerable<string> commandMarkers;
        private bool connected;
        private DiscordClient discordClient;
        private CommandsNextExtension commands;
        public bool Connected {
            get => connected;
        }
        public DiscordHelper(string token, IEnumerable<string> commandMarkers, IConfiguration config, ILogger<HomieBot> logger, IServiceProvider services)
        {
            this.token = token;
            this.commandMarkers = commandMarkers;
            this.config = config;
            this.logger = logger;
            this.services = services;
            connected = false;
        }

        public async Task Initialize()
        {
            logger.LogInformation("Starting up main discord client");
            discordClient = new DiscordClient( new DiscordConfiguration(){
                AutoReconnect = true,
                Token = token,
                TokenType = TokenType.Bot,
                LogLevel = DSharpPlus.LogLevel.Info,
            });
            commands = discordClient.UseCommandsNext(new CommandsNextConfiguration(){
                CaseSensitive = false,
                EnableDefaultHelp = true,
                StringPrefixes = commandMarkers,
                Services = services
            });
            logger.LogInformation("Registering custom parser");
            //commands.RegisterConverter(new StringArrayParamConverter());
            logger.LogInformation("Registering Baseline Commands");
            commands.RegisterCommands<HomieCommands>();
            logger.LogInformation("Parsing gimmicks");
            HomieCommands hc = new HomieCommands((Random)services.GetService(typeof(Random)),logger,config);
            //var childgimmicks = config.GetSection("Gimmicks").GetChildren();
            var Gimmicks = config.GetSection("Gimmicks").Get<IEnumerable<Gimmick>>();
            logger.LogInformation("Registering Gimmicks");
            commands.RegisterCommands(hc.GetDynamicGimmickCommands(Gimmicks));
            discordClient.MessageCreated += async (MessageCreateEventArgs message) => 
            {
                if(message.MentionedUsers.Contains(discordClient.CurrentUser))
                {
                    await message.Channel.TriggerTypingAsync();
                    logger.LogInformation("Mentioned by name figuring out what to do with that");
                    switch (message.Message.Content){
                        case var m when new Regex(@"\b(why)\b").IsMatch(m):
                            logger.LogInformation("Matched on why, so making an excuse");
                            var context = discordClient.GetCommandsNext().CreateContext(message.Message,"::",discordClient.GetCommandsNext().RegisteredCommands["campaign"]);
                            await discordClient.GetCommandsNext().RegisteredCommands["campaign"].ExecuteAsync(context);
                            break;
                        default:
                            logger.LogInformation("I was pinged but couldn't find a match command, returning help instructions");
                            await message.Channel.SendMessageAsync($"{message.Author.Mention} I don't know what to do with that, but you can use the command {commandMarkers.FirstOrDefault()}help for some help");
                            break;
                    }
                }
                return;
            };
            logger.LogInformation("Trying Connect");
            try
            {
                await discordClient.ConnectAsync();
                connected = true;
            }
            catch(Exception e)
            {
                logger.LogError(e,"Exception trying to connect: {errorMessage}",e.Message);
            }
        }

        public async Task Disconnect()
        {
            if(connected)
            {
                await discordClient.DisconnectAsync();
                connected = false;
            }
        }

        public async Task<bool> ReConnect()
        {
            try
            {
                await discordClient.ReconnectAsync();
                return true;
            }
            catch(Exception e)
            {
                logger.LogError(e,"Exception trying to reconnect: {errorMessage}", e.Message);
                return false;
            }
            
        }

    }
}