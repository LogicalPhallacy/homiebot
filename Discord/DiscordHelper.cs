using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using Homiebot.Discord.Commands;
using Homiebot.Discord.Voice;
using Homiebot.Models;
using Homiebot.Images;
using System.Diagnostics;

namespace Homiebot.Discord 
{
    public class DiscordHelper
    {
        public const int CharacterLimit = 1900;
        private readonly ILogger<HomieBot> logger;
        private readonly IConfiguration config;
        private readonly IServiceProvider services;
        private string token;
        private string[] commandMarkers;
        private IEnumerable<RESTGimmick> restGimmicks;
        private bool connected;
        private DiscordClient discordClient;
        private CommandsNextExtension commands;
        private VoiceNextExtension voiceNext;
        private BotConfig homiebotConfig;
        public bool Connected {
            get => connected;
        }
        public DiscordHelper(string token, IEnumerable<string> commandMarkers, IConfiguration config, ILogger<HomieBot> logger, IServiceProvider services)
        {
            this.token = token;
            this.commandMarkers = commandMarkers.ToArray();
            this.config = config;
            this.logger = logger;
            this.services = services;
            connected = false;
        }
        private T getService<T>(){
            return (T)services.GetService(typeof(T));
        }
        public async Task Initialize()
        {
            homiebotConfig = config.GetSection("BotConfig").Get<BotConfig>();
            logger.LogInformation("Starting up main discord client");
            discordClient = new DiscordClient( new DiscordConfiguration(){
                AutoReconnect = true,
                Token = token,
                TokenType = TokenType.Bot,
                Intents =
                    DiscordIntents.AllUnprivileged |
                    DiscordIntents.MessageContents
            });
            commands = discordClient.UseCommandsNext(new CommandsNextConfiguration(){
                CaseSensitive = false,
                EnableDefaultHelp = true,
                StringPrefixes = commandMarkers,
                UseDefaultCommandHandler = true,
                Services = services,
                //EnableDms = false,
                //EnableMentionPrefix = true,
            });
            logger.LogInformation("Registering custom parser");
            //commands.RegisterConverter(new StringArrayParamConverter());
            //where the commands are registered!
            logger.LogInformation("Registering Baseline Commands");
            commands.SetHelpFormatter<CustomHelpFormatter>();
            commands.RegisterCommands<HomieCommands>();
            commands.RegisterCommands<ImageMemeCommands>();
            commands.RegisterCommands<ChatCommands>();
            commands.RegisterCommands<DiceCommands>();
            logger.LogInformation("Parsing gimmicks");
            HomieCommands hc = homiebotConfig.UseVoice ? 
            new HomieCommands(getService<Random>(),logger,config,getService<ITextToSpeechHelper>()) :
            new HomieCommands(getService<Random>(),logger,config);
            ImageMemeCommands ic = new ImageMemeCommands(logger,config,getService<IImageStore>(),getService<IImageProcessor>(), getService<Random>());
            //ChatCommands cc = new ChatCommands(logger, getService<ITextAnalyzer>());
            //var childgimmicks = config.GetSection("Gimmicks").GetChildren();
            var Gimmicks = config.GetSection("Gimmicks").Get<IEnumerable<Gimmick>>();
            restGimmicks = config.GetSection("RestGimmicks").Get<IEnumerable<RESTGimmick>>();
            logger.LogInformation("Registering Gimmicks");
            commands.RegisterCommands(hc.GetDynamicGimmickCommands(Gimmicks));
            commands.RegisterCommands(ic.GetDynamicImageCommands());
            foreach(var restGimmick in restGimmicks){
                restGimmick.Initialize();
            }
            logger.LogInformation("Registering reactions");
            discordClient.MessageReactionAdded += hc.ProcessReaction;
            discordClient.MessageReactionAdded += ic.ProcessReaction;
            if(homiebotConfig.UseVoice)
            {
                logger.LogInformation("Registering Voice Commands");
                voiceNext = discordClient.UseVoiceNext(
                    new VoiceNextConfiguration{
                        EnableIncoming = false,
                        AudioFormat = new AudioFormat(48000,1,VoiceApplication.Voice)
                    }
                );
                commands.RegisterCommands<VoiceCommands>();
            }
            if(homiebotConfig.UseBrain)
            {
                logger.LogInformation("Registering memory commands");
                commands.RegisterCommands<MemoryCommands>();
            }
            discordClient.MessageCreated += HandleMessage;
            // TODO: Set up event emission for successful command runs
            // commands.CommandExecuted += (ext, args) => {}
            commands.CommandErrored += async (ext, args) => {
                logger.LogError(args?.Exception ?? new Exception("Unknown Exception"), "Failed to execute command {commandName}", args?.Command?.Name ?? "Unknown");
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

        private async Task HandleMessage(DiscordClient sender, MessageCreateEventArgs message)
        {
            // We don't want to process our own messages
            // or messages from other bots
            if(message.Message.Author.IsBot || message.Message.Author.IsCurrent)
            {
                return;
            }
            foreach(var command in homiebotConfig.IgnoredCommands)
            {
                if(message.Message.Content.StartsWith(command, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogInformation("Ignoring command {commandName}", command);
                    return;
                }
            }
            foreach(var marker in commandMarkers){
                if(message.Message.Content.StartsWith(marker)){
                    return;
                }
            }
            var songlinkHandle = message.HandleSongLinks(sender, logger);
            await message.HandleMemorableKeywords(sender, logger);
            if(message.MentionedUsers.Contains(discordClient.CurrentUser))
            {
                using var mentionActivity = Homiebot.Web.TelemetryHelpers.StartActivity("HomiebotMention");
                await message.Channel.TriggerTypingAsync();
                logger.LogInformation("Mentioned by name figuring out what to do with that");
                bool handled = false;
                handled = await message.HandleHomieMentionCommands(sender,logger);
                if (!handled){
                    handled = await handleRestGimmicks(message);
                }
                // subsequent handler extension methods go in if clauses below
                if(!handled){handled = await message.HandleMemoryMentionCommands(logger);}
                // finally if they're still unhandled do the default
                if(!handled)
                {
                    mentionActivity?.SetStatus(ActivityStatusCode.Error, "NoSuccessfulHandlers")?.Stop();
                    logger.LogInformation("I was pinged but couldn't find a match command, returning help instructions");
                    //await message.Channel.SendMessageAsync($"{message.Author.Mention} I don't know what to do with that, but you can use the command {commandMarkers.FirstOrDefault()}help for some help");
                }else{
                    mentionActivity?.SetStatus(ActivityStatusCode.Ok)?.Stop();
                }
            }
            await songlinkHandle.WaitAsync(TimeSpan.FromSeconds(5));
            return;
        }

        private async Task<bool> handleRestGimmicks(MessageCreateEventArgs message)
        {
            foreach(var gim in restGimmicks){
                if(gim.ShouldTriggerGimmick(message.Message.Content))
                {
                    await gim.RunRESTGimmick(message.Message);
                    return true;
                }
            }
            return false;
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
//where the custom help format is setup
    public class CustomHelpFormatter : DefaultHelpFormatter
    {   
        public CustomHelpFormatter(CommandContext ctx) : base(ctx) { }

        public override CommandHelpMessage Build()
        {   
            EmbedBuilder.Color = DiscordColor.SpringGreen;
            return base.Build();
        }
    }
}