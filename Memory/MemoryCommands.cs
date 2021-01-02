using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using System.Text.RegularExpressions;

namespace homiebot.memory
{
    public static class MemoryCommandExtensions
    {
        public static async Task<bool> HandleMemorableKeywords(this MessageCreateEventArgs message, ILogger logger)
        {
            switch (message.Message.Content)
            {
                case var m when new Regex(@"(acab includes\b)(.+)").IsMatch(m):
                    var acabcontent = new Regex(@"(acab includes\b)(.+)").Match(m).Groups[2].Value;
                    var acabcontext = message.Client.GetCommandsNext().CreateContext(message.Message,"::",message.Client.GetCommandsNext().RegisteredCommands["acab"],acabcontent);
                    await message.Client.GetCommandsNext().RegisteredCommands["acab"].ExecuteAsync(acabcontext);
                    return true;
                case var m when new Regex(@"(.+)\b(is a cia psyop\b)").IsMatch(m):
                    var matchcontent = new Regex(@"(.+)\b(is a cia psyop\b)").Match(m).Groups[1].Value;
                    var context = message.Client.GetCommandsNext().CreateContext(message.Message,"::",message.Client.GetCommandsNext().RegisteredCommands["psyop"],matchcontent);
                    await message.Client.GetCommandsNext().RegisteredCommands["psyop"].ExecuteAsync(context);
                    return true;
                case var m when new Regex(@"(.+)\b(found on wish.com\b)").IsMatch(m):
                    var wishcontent = new Regex(@"(.+)\b(found on wish.com\b)").Match(m).Groups[1].Value;
                    var wishcontext = message.Client.GetCommandsNext().CreateContext(message.Message,"::",message.Client.GetCommandsNext().RegisteredCommands["wish"],wishcontent);
                    await message.Client.GetCommandsNext().RegisteredCommands["wish"].ExecuteAsync(wishcontext);
                    return true;
            }
            return false;
        }
        // Extension to the discord message class to handle "conversational" requests in this commands lib
        public static async Task<bool> HandleMemoryMentionCommands(this MessageCreateEventArgs message, ILogger logger)
        {
            return false;
        }
    }
    public class MemoryCommands : BaseCommandModule
    {
        private IMemoryProvider memory;
        private Random random;
        private ILogger logger;
        private IConfiguration configuration;
        private BotConfig botConfig;
        public MemoryCommands(IMemoryProvider memoryProvider, Random random, ILogger<HomieBot> logger, IConfiguration config)
        {
            this.memory = memoryProvider;
            this.random = random;
            this.logger = logger;
            this.configuration = config;
            botConfig = configuration.GetSection("BotConfig").Get<BotConfig>();
        }

        [Command("remember")]
        [Description("remembers something for you")]
        public async Task Remember(CommandContext context, string key, [RemainingText]string value)
        {
            await context.TriggerTypingAsync();
            string guildedKey = $"{context.Guild.Id}-{key}";
            // check if the item exists first
            MemoryItem existing = memory.GetItem<MemoryItem>(guildedKey);
            if(existing == null)
            {
                existing = new MemoryItem(guildedKey);
                existing.Owner = context.User.Id.ToString();
                existing.Message = value;
                existing.GuildName = context.Guild.Id.ToString();
                if(await rememberItem<MemoryItem>(existing))
                {
                    await context.RespondAsync("You betcha");
                }
                else
                {
                    await context.RespondAsync("Don't think I will...");
                }
            }
        }
        [Command("forget")]
        [Description("forgets something you wanted homiebot to remember")]
        public async Task Forget(CommandContext context, string key)
        {
            await context.TriggerTypingAsync();
            string guildedKey = $"{context.Guild.Id}-{key}";
            MemoryItem existing = await memory.GetItemAsync<MemoryItem>(guildedKey);
            if(existing != null)
            {
                if(existing.Owner == context.User.Id.ToString() || botConfig.Admins.Contains(context.User.Username))
                {
                    if(await forgetItem<MemoryItem>(existing))
                    {
                        await context.RespondAsync("I'm not saying another word about it");
                    }
                    else
                    {
                        await context.RespondAsync("I don't think I can do that...");
                    }
                }
                else
                {
                    var owner = await context.Client.GetUserAsync(ulong.Parse(existing.Owner));
                    await context.RespondAsync($"Sorry homie, only {owner.Username} can delete this");
                }
            }
            else
            {
                await context.RespondAsync($"I don't know anything about {key}, sorry");
            }
        }
        [Command("recall")]
        [Description("Gets something from memory")]
        public async Task Recall(CommandContext context, string key)
        {
            await context.TriggerTypingAsync();
            string guildedKey = $"{context.Guild.Id}-{key}";
            MemoryItem existing = await memory.GetItemAsync<MemoryItem>(guildedKey);
            if(existing != null)
            {
                await context.RespondAsync(existing.Message);
            }
            else
            {
                await context.RespondAsync($"I don't know anything about {key}, sorry");
            }
        }
        public async Task<bool> rememberItem<T>(T item) where T : StoredItem
        {
            try
            {
                await memory.StoreItemAsync<T>(item);
                return true;
            }
            catch (Exception e)
            {
                logger.LogError(e,"Failed to store item");
                return false;
            }
        }

        public async Task<bool> forgetItem<T>(T item) where T : StoredItem
        {
            try
            {
                await memory.RemoveItemAsync<T>(item);
                return true;
            }
            catch (Exception e)
            {
                logger.LogError(e,"Failed to delete item");
                return false;
            }
        }

        [Command("acab")]
        [Description("Run by itself to learn which cops are bastards, run with a parameter to include that fact")]
        public async Task ACAB(CommandContext context, [RemainingText]string? info)
        {
            await context.TriggerTypingAsync();
            if(string.IsNullOrWhiteSpace(info))
            {
                var result = await getGimmickMessage("acab");
                if(string.IsNullOrWhiteSpace(result))
                {
                    await context.RespondAsync("ACAB includes many things that I don't know about yet. Tell me some of them.");
                }
                else
                {
                    await context.RespondAsync($"Sorry, but ACAB includes {result}");
                }
            }
            else 
            {
                if(await rememberGimmickMessage(context.Guild.Id.ToString(),context.Client.CurrentUser.Id.ToString(),"acab",info.Trim()))
                {
                    await context.RespondAsync("I've made a note of that");
                }
                else
                {
                    await context.RespondAsync("Couldn't be, because I can't remember that");
                }
            }
        }

        [Command("psyop")]
        [Description("Some things are CIA Psy Ops, run this command to see which ones are, or with a thing that is a CIA Psy Op to add it!")]
        public async Task PsyOp(CommandContext context, [RemainingText]string? info)
        {
            await context.TriggerTypingAsync();
            if(string.IsNullOrWhiteSpace(info))
            {
                var result = await getGimmickMessage("psyop");
                if(string.IsNullOrWhiteSpace(result))
                {
                    await context.RespondAsync("The CIA itself is a CIA PsyOp, that's the best I've got until we add some things");
                }
                else
                {
                    if(result.EndsWith('s') || result.EndsWith('S'))
                    {
                        await context.RespondAsync($"{result} are a CIA PsyOp.");
                    }
                    else
                    {
                        await context.RespondAsync($"{result} is a CIA PsyOp.");
                    }
                }
            }
            else 
            {
                if(await rememberGimmickMessage(context.Guild.Id.ToString(),context.Client.CurrentUser.Id.ToString(),"psyop",info.Trim()))
                {
                    await context.RespondAsync("I've made a note of that");
                }
                else
                {
                    await context.RespondAsync("Couldn't be, because I can't remember that");
                }
            }
        }

        [Command("wish")]
        [Description("What can you find on wish dot com, or add a link to something you found on wish.com")]
        public async Task Wish(CommandContext context, [RemainingText]string? info)
        {
            await context.TriggerTypingAsync();
            if(string.IsNullOrWhiteSpace(info))
            {
                var result = await getGimmickMessage("wish");
                if(string.IsNullOrWhiteSpace(result))
                {
                    await context.RespondAsync("I haven't seen anything on wish.com you shoud add some things");
                }
                else
                {
                    await context.RespondAsync($"{result} as seen on wish.com");
                }
            }
            else 
            {
                if(await rememberGimmickMessage(context.Guild.Id.ToString(),context.Client.CurrentUser.Id.ToString(),"wish",info.Trim()))
                {
                    await context.RespondAsync("I've made a note of that");
                }
                else
                {
                    await context.RespondAsync("Couldn't be, because I can't remember that");
                }
            }
        }

        private async Task<bool> rememberGimmickMessage(string guildId, string owner, string gimmickKey, string info)
        {
            string stampedKey = $"{gimmickKey}-{DateTime.Now.ToString("yyMMddHHmmSS")}";
            var store = new MemoryItem(stampedKey);
            store.Owner = owner;
            store.Message = info;
            store.GuildName = guildId;
            try
            {
                await memory.StoreItemAsync<MemoryItem>(store);
                return true;
            }
            catch (Exception e)
            {
                logger.LogError(e,"Couldn't save item");
                return false;
            }
        }

        private async Task<string> getGimmickMessage(string gimmickKey)
        {
            var Results = memory.FindItems<MemoryItem>(
                i=> i.Key.StartsWith(gimmickKey)
            );
            var count = Results.Count(); 
            if(count == 0)
            {
                return null;
            }else{
                return Results.ElementAt(random.Next(0,count-1)).Message;
            }
        }
        // Placeholdering remind and tell until I am good with the dbs workings
        // And have time to setup the local cache and updating it because this featur is expensive without it

        // [Command("tell")]
        // [Description("Can be used to tell another user something next time they speak")]
        // public async Task Tell(CommandContext context, string key)
        // {
        //     
        // }
        
        // [Command("remind")]
        // [Description("Reminds someone something")]
        // public async Task Remind(CommandContext context, string key)
        // {
        //     
        // }
    }
}