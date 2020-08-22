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
using System.ComponentModel;
using System.Text.Json;
using System.IO;
using DSharpPlus.Entities;

namespace homiebot 
{
    public class StringArrayParamConverter : IArgumentConverter<string[]>
    {
        public Task<Optional<string[]>> ConvertAsync(string value, CommandContext ctx)
        {
            string val = value;
            if(string.IsNullOrWhiteSpace(value))
            {
                return Task.FromResult(Optional.FromValue<string[]>(new string[]{}));  
            }
            return Task.FromResult(Optional.FromValue<string[]>(value.Split(" ")));
        }
    }
    public class HomieCommands : BaseCommandModule
    {
        public delegate Task RunGimmick(CommandContext ctx, params string[] input);
        private readonly Random random;
        private readonly ILogger logger;
        private readonly IConfiguration config;
        public HomieCommands(Random random, ILogger<HomieBot> logger, IConfiguration config)
        {
            this.random = random;
            this.logger = logger;
            this.config = config;
        }
        public CommandBuilder[] GetDynamicGimmickCommands(IEnumerable<Gimmick> gimmicks)
        {
            var commands = new List<CommandBuilder>();
            foreach(var gimmick in gimmicks)
            {
                gimmick.Inject(random,logger);
                commands.Add(new CommandBuilder()
                //.WithAlias(gimmick.Command)
                .WithName(gimmick.Command)
                .WithDescription(gimmick.Description)
                .WithOverload(
                    new CommandOverloadBuilder(new RunGimmick(gimmick.RunGimmick)).WithPriority(0)
                    )
                );
            }
            return commands.ToArray();
        }

        [Command("reload")]
        public async Task ReloadGimmicks(CommandContext context)
        {
            await context.RespondAsync("Reloading gimmicks, please wait");
            // Get the gimmick list and try and unregister them
            var Gimmicks = config.GetSection("Gimmicks").Get<IEnumerable<Gimmick>>();
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
        }
    }
}