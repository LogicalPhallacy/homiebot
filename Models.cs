using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.Logging;

namespace homiebot
{
    public class GimmickFile 
    {
        public IEnumerable<Gimmick> Gimmicks {get; set;}
    }
    public class Gimmick
    {
        public string Command {get; set;}
        public string Description {get; set;}
        public IEnumerable<string> ReplacementStrings {get; set;}
        public string ArgSplitter {get;set;}
        public int ArgCount {get;set;}

        private Random random;
        private ILogger logger;

        public void Inject(Random random, ILogger logger){
            this.random = random;
            this.logger = logger;
        }
        public string Replace(params string[] args)
        {
            var gimmick = this;
            string retstr = gimmick.ReplacementStrings.OrderBy(x => random.Next()).First();
            if(args != null && args.Length > 0)
            {
                string joined = string.Join(' ',args);
                if(! string.IsNullOrEmpty(gimmick.ArgSplitter))
                {
                    string[] newargs = joined.Split(',');
                    if(newargs.Length < gimmick.ArgCount){
                        retstr = $"Homie don't play that.\nI need {gimmick.ArgCount} things separated by {gimmick.ArgSplitter}";
                        return retstr;
                    }
                    for (int i = 0; i < gimmick.ArgCount; i++)
                    {
                        retstr = retstr.Replace($"@REPLACEMENT{i+1}@",newargs[i]);
                    }
                }else{
                    for (int i = 0; i < gimmick.ArgCount; i++)
                    {
                        retstr = retstr.Replace($"@REPLACEMENT{i+1}@",joined);
                    }
                }
            }
            return retstr;
        }
        public async Task RunGimmick(CommandContext ctx, params string[] args)
        {
            await ctx.TriggerTypingAsync();
            logger.LogInformation("Got a gimmick command: {gimmick}", this.Command);
            logger.LogInformation("From Guild: {Guild} Channel:{Channel}", ctx.Guild.Name,ctx.Channel.Name);
            logger.LogInformation("Params for command are {params}", string.Join(',',args));
            string returnstring = this.Replace(args);
            await ctx.RespondAsync(returnstring);
        }
    }

    public class BotConfig
    {
        public string DiscordToken {get; set;}
        public IEnumerable<string> CommandPrefixes{get;set;}
    }
}