using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Builders;
using DSharpPlus.CommandsNext.Converters;
using Homiebot.Discord.Voice;
using Homiebot.Helpers;
using Homiebot.Models;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Homiebot.Web;

namespace Homiebot.Discord.Commands
{
    public class StringArrayParamConverter : IArgumentConverter<string[]>
    {
        public Task<Optional<string[]>> ConvertAsync(string value, CommandContext ctx)
        {
            string val = value;
            if(string.IsNullOrWhiteSpace(value))
            {
                return Task.FromResult(Optional.FromValue<string[]>([]));  
            }
            return Task.FromResult(Optional.FromValue(value.Split(" ")));
        }
    }
    public static class HomieMessageExtensions
    {
        // Extension to the discord message class to handle "conversational" requests in this commands lib
        public static async Task<bool> HandleHomieMentionCommands(this MessageCreateEventArgs message, DiscordClient sender, ILogger logger)
        {
            switch (message.Message.Content){
                case var m when RegexHelper.ExcuseRegex().IsMatch(m):
                    logger.LogInformation("Matched on why, so making an excuse");
                    await ReactToMessage(message.Message, sender, "excuse");
                    return true;
                case var m when RegexHelper.ThanksRegex().IsMatch(m):
                    await message.Message.CreateReactionAsync(DiscordEmoji.FromName(sender,":IsForMe:"));
                    return true;
                case var m when RegexHelper.RudeRegex().IsMatch(m):
                    await message.Message.RespondAsync($"Fuck you too, {message.Author.Mention}");
                    return true;
                case var m when m.Contains("420") || m.Contains("69"):
                    await ReactToMessage(message.Message, sender, "nice");
                    return true;
                default:
                    return false;
            }
        }
        public static async Task<CommandResult> ReactToMessage(DiscordMessage message, DiscordClient sender, string reactWithCommandName)
        {
            var context = sender.GetCommandsNext().CreateContext(message,"::",sender.GetCommandsNext().RegisteredCommands[reactWithCommandName]);
            return await sender.GetCommandsNext().RegisteredCommands[reactWithCommandName].ExecuteAsync(context);
        }
    }
    [ModuleLifespan(ModuleLifespan.Singleton)]
    public class HomieCommands : BaseCommandModule
    {
        public delegate Task RunGimmick(CommandContext ctx, params string[] input);
        private readonly Random random;
        private readonly ILogger logger;
        private readonly IConfiguration config;
        private readonly BotConfig botConfig;
        private IEnumerable<Gimmick> Gimmicks;
        private IEnumerable<ReactionConfig> reactionConfigs;
        private ITextToSpeechHelper? textToSpeechHelper;
        private const string clapchar = "👏";
        public HomieCommands(Random random, ILogger<HomieBot> logger, IConfiguration config, ITextToSpeechHelper? textToSpeechHelper = null)
        {
            this.random = random;
            this.logger = logger;
            this.config = config;
            botConfig = config.GetSection("BotConfig").Get<BotConfig>();
            
            this.textToSpeechHelper = botConfig.UseVoice ? textToSpeechHelper : null;
            InitializeGimmicks();
            reactionConfigs = config.GetSection("ReactionPacks").Get<IEnumerable<ReactionConfig>>();
        }
        public void InitializeGimmicks()
        {
            Gimmicks = config.GetSection("Gimmicks").Get<IEnumerable<Gimmick>>();
            foreach(var gimmick in Gimmicks)
            {
                gimmick.Inject(random,logger,textToSpeechHelper);
            }
        }
        public CommandBuilder[] GetDynamicGimmickCommands(IEnumerable<Gimmick> gimmicks)
        {
            var commands = new List<CommandBuilder>();
            foreach(var gimmick in gimmicks)
            {
                gimmick.Inject(random,logger,textToSpeechHelper);
                commands.Add(new CommandBuilder()
                //.WithAlias(gimmick.Command)
                .WithName(gimmick.Command)
                .WithDescription(gimmick.Description)
                .WithOverload(
                    new CommandOverloadBuilder(new RunGimmick(gimmick.RunGimmick)).WithPriority(0)
                    )
                );
                if(gimmick.CanVoice && botConfig.UseVoice)
                {
                    commands.Add(
                        new CommandBuilder()
                        .WithName($"say{gimmick.Command}")
                        .WithDescription($"{gimmick.Description} over voice chat")
                        .WithOverload(
                            new CommandOverloadBuilder(new RunGimmick(gimmick.SpeakGimmick)).WithPriority(0)
                        )
                    );
                }
            }
            return commands.ToArray();
        }

        public async Task ProcessReaction(DiscordClient sender, MessageReactionAddEventArgs messageReaction)
        {
            DiscordMessage fullmessage = null;
            switch(messageReaction.Emoji.GetDiscordName())
            {
                case ":reverse_uno_card:":
                    // turns out the message we react to doesn't come with a body for some inane reason
                    // lets look it up I guess
                    if(string.IsNullOrWhiteSpace(messageReaction.Message.Content))
                    {
                        fullmessage = await messageReaction.Channel.GetMessageAsync(messageReaction.Message.Id);
                    }else{
                        fullmessage = messageReaction.Message;
                    }
                    await messageReaction.Message.RespondAsync(fullmessage.Content.ToMockingCase(random));
                    break;
                case ":rainbow_reverse_card:" or ":uwuno:":
                    // turns out the message we react to doesn't come with a body for some inane reason
                    // lets look it up I guess
                    
                    if(string.IsNullOrWhiteSpace(messageReaction.Message.Content))
                    {
                        fullmessage = await messageReaction.Channel.GetMessageAsync(messageReaction.Message.Id);
                    }else{
                        fullmessage = messageReaction.Message;
                    }
                    await messageReaction.Message.RespondAsync(fullmessage.Content.ToUwuCase());
                    break;
                case var mr when reactionConfigs.Where(rc=>rc.TriggerReaction == mr).FirstOrDefault() != null:
                    await messageReaction.Channel.TriggerTypingAsync();
                    var react = reactionConfigs.Where(rc=>rc.TriggerReaction == mr).FirstOrDefault();
                    logger.LogInformation("Saw a reaction that should trigger reactionpack: {packname}",react.ReactionName);
                    foreach(var reaction in react.Reactions)
                    {
                        await messageReaction.Message.CreateReactionAsync(DiscordEmoji.FromName(sender,reaction));
                    }
                    break;
                default:
                    break;
            }
        }

        [Command("reactions")]
        [Description("List the emoji groupings that will be triggered by one emoji when you react to a message with it")]
        public async Task GetReactionTriggers(CommandContext context)
        {
            using var commandRun = TelemetryHelpers.StartActivity(context.Command.Name);
            await context.TriggerTypingAsync();
            string outmessage = string.Empty;
            foreach(ReactionConfig react in reactionConfigs)
            {
                outmessage+= $"{react.ReactionName} - Trigger with: {react.TriggerReaction}, homiebot will add {string.Join(',',react.Reactions)}\n";
            }
            await context.RespondAsync(outmessage);
            commandRun?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok)?.Stop();
        }

        [Command("reload")]
        public async Task ReloadGimmicks(CommandContext context)
        {
            using var commandRun = TelemetryHelpers.StartActivity(context.Command.Name);
            await context.RespondAsync("Reloading gimmicks, please wait");
            // Get the gimmick list and try and unregister them
            Gimmicks = config.GetSection("Gimmicks").Get<IEnumerable<Gimmick>>();
            var CommandList = new List<Command>();
            foreach (var gimmick in Gimmicks)
            {
                KeyValuePair<string,Command> command;
                try
                {
                    command = context.CommandsNext.RegisteredCommands.Where(kvp => kvp.Key == gimmick.Command).FirstOrDefault();
                }
                catch(Exception e)
                {
                    logger.LogError(e, "Couldn't unregister command: {Command}", gimmick.Command);
                    continue;
                }
                if(command.Value != null && command.Key == gimmick.Command)
                {
                    CommandList.Add(command.Value);
                }
            }
            context.CommandsNext.UnregisterCommands(CommandList.ToArray()); 
            // Reload our gimmicks
            context.CommandsNext.RegisterCommands(GetDynamicGimmickCommands(Gimmicks));
            await context.RespondAsync("gimmicks reloaded!");
            commandRun?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok)?.Stop();
        }
#nullable enable
        [Command("clapback")]
        [Description("you👏know👏what👏this👏does")]
        public async Task ClapBack(CommandContext context, [RemainingText] string? text)
        {
            using var commandRun = TelemetryHelpers.StartActivity(context.Command.Name);
            await context.TriggerTypingAsync();
            if(!string.IsNullOrWhiteSpace(text))
            {
                var resp = text.Replace(" ",clapchar);
                resp += clapchar;
                await context.RespondAsync(resp);
            }
            commandRun?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok)?.Stop();
        }
#nullable disable
        [Command("define")]
        [Description("Fetches a definition from urban dictionary")]
        public async Task Define(CommandContext context, [RemainingText] string text)
        {
            using var commandRun = TelemetryHelpers.StartActivity(context.Command.Name);
            await context.TriggerTypingAsync();
            DiscordEmbed d = null;
            try{
                d = await UrbanDictionaryHelper.GetDefinition(text);
            }catch(Exception e){
                await context.RespondAsync($"Sorry homie, error looking up {text}\n{e.Message}");
                commandRun?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, e.Message)?.Stop();
                return;
            }
            await context.RespondAsync(embed:d);
            commandRun?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok)?.Stop();
        }
        [Command("slap")]
        [Description("Slaps a user with a fish")]
        public async Task Slap(CommandContext context, [RemainingText] string text)
        {
            using var commandRun = TelemetryHelpers.StartActivity(context.Command.Name);
            await context.TriggerTypingAsync();
            await context.RespondAsync($"*slaps {text} with a wet trout*");
            commandRun?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok)?.Stop();
        }
        [Command("mock")]
        [Description("Gets some mocking text")]
        public async Task Mock(CommandContext context, [RemainingText]string text)
        {
            using var commandRun = TelemetryHelpers.StartActivity(context.Command.Name);
            await context.TriggerTypingAsync();
            await context.RespondAsync(text.ToMockingCase(random));
            commandRun?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok)?.Stop();
        }
        [Command("uwu")]
        [Description("makes youw text go uwu")]
        public async Task Uwu(CommandContext context, [RemainingText]string text)
        {
            using var commandRun = TelemetryHelpers.StartActivity(context.Command.Name);
            await context.TriggerTypingAsync();
            await context.RespondAsync(text.ToUwuCase());
            commandRun?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok)?.Stop();
        }
        [Command("block")]
        [Description("For when you want to make a point")]
        public async Task Block(CommandContext context, [RemainingText]string text)
        {
            using var commandRun = TelemetryHelpers.StartActivity(context.Command.Name);
            await context.TriggerTypingAsync();
            await context.RespondAsync("```\n"+text.ToBlockText()+"\n```");
            commandRun?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok)?.Stop();
        }
        [Command("cls")]
        [Description("Make homiebot delete the last thing he said")]
        public async Task Clear(CommandContext context)
        {
            using var commandRun = TelemetryHelpers.StartActivity(context.Command.Name);
            // get the channel 
            await context.TriggerTypingAsync();
            var messages = await context.Channel.GetMessagesAsync();
            var lastmessage = messages.Where(dm => dm.Author.Id == context.Client.CurrentUser.Id).OrderByDescending( dm => dm.CreationTimestamp).First();
            if(lastmessage != null){
                try
                {
                    await lastmessage.DeleteAsync();
                    await context.RespondAsync("Its like it never happened...");
                    commandRun?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok)?.Stop();
                }
                catch(Exception e)
                {
                    commandRun?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, e.Message)?.Stop();
                    await context.RespondAsync($"The bleach, it does nothing: {e.Message}");
                }
            }
        }
        
        [Command("ias")]
        [Description("Homiebot will generate an its always sunny title card")]
        public async Task AlwaysSunny(CommandContext context, [RemainingText]string text)
        {
            using var commandRun = TelemetryHelpers.StartActivity(context.Command.Name);
            await context.TriggerTypingAsync();
            string title = string.Empty;
            try
            {
                title = await AlwaysSunnyHelper.GenerateTitleUrl(text);
            }
            catch(Exception e)
            {
                await context.RespondAsync($"Sorry homie, an error\n{e.Message}");
                commandRun?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, e.Message)?.Stop();
                return;
            }
            if(string.IsNullOrWhiteSpace(title))
            {
                await context.RespondAsync("Sorry homie, I couldn't get a title card for that");
                commandRun?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, "No title returned")?.Stop();
                return;
            }
            await context.RespondAsync(title);
            commandRun?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok)?.Stop();
        }
    }
}