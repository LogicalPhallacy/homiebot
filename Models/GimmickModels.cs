using System.Linq;
using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Homiebot.Discord.Voice;
namespace Homiebot.Models
{
    public struct GimmickRun
    {
        public string Name;
        public string Message;
    }
    public class Gimmick
    {
        public string Command {get; set;}
        public string Description {get; set;}
        public IEnumerable<string> ReplacementStrings {get; set;}
        private HashSet<string> usedStrings;
        public string? StringTerminator {get;set;}
        public string ArgSplitter {get;set;}
        public int ArgCount {get;set;}
        public bool Injected{get; set;}

        public bool CanVoice{get;set;}

        private Random random;
        private ILogger logger;

        private ITextToSpeechHelper textToSpeechHelper;

        public Gimmick()
        {
            Injected = false;
            usedStrings = new ();
        }
        public void Inject(Random random, ILogger logger, ITextToSpeechHelper textToSpeechHelper){
            this.random = random;
            this.logger = logger;
            this.textToSpeechHelper = textToSpeechHelper;
            Injected = true;
        }
        public string Replace(params string[] args)
        {
            var gimmick = this;
            if(usedStrings.Count == ReplacementStrings.Count()){
                usedStrings = new ();
            }
            var retstr = gimmick.ReplacementStrings
                .Where(str=> !usedStrings.Contains(str))
                .OrderBy(x => random.Next())
                .First();
            usedStrings.Add(retstr);
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
            if(!string.IsNullOrWhiteSpace(StringTerminator))
            {
                retstr+=StringTerminator;
            }
            return retstr;
        }
        public async Task RunGimmick(CommandContext ctx, params string[] args)
        {
            await ctx.TriggerTypingAsync();
            logger.LogInformation("Got a gimmick command: {gimmick}\nFrom Guild: {Guild} Channel:{Channel}", this.Command,ctx.Guild.Name,ctx.Channel.Name);
            //logger.LogInformation("Params for command are {params}", string.Join(',',args));
            string returnstring = ReplaceEmoji(this.Replace(args),ctx);
            foreach(var message in returnstring.SplitForDiscord()){
                await ctx.RespondAsync(message);
            }
        }

        public async Task SpeakGimmick(CommandContext context, params string[] args)
        {
            await context.TriggerTypingAsync();
            if(CanVoice && textToSpeechHelper != null)
            {
                await SpeechHelper.Speak(textToSpeechHelper,context,Replace(args),overrideLimit: true);
                return;
            }
            await context.RespondAsync("Some things don't need saying homie");
        }

        private static string ReplaceEmoji(string str, CommandContext ctx)
        {
            if(str.Contains(':')){
                var substr = str.Substring(str.IndexOf(':')+1);
                if(substr.Contains(':')){
                    var parsable = substr.Substring(0,substr.IndexOf(':'));
                    if(parsable.Contains(' ')){
                        return str.Substring(0,str.IndexOf(':'))+(ReplaceEmoji(substr,ctx));
                    }
                    try{
                        var emoji = DiscordEmoji.FromName(ctx.Client,$":{parsable}:");
                        if(emoji.IsAvailable){
                            return ReplaceEmoji(str.Replace($":{parsable}:",$"&PLACEHOLD{parsable}&"),ctx).Replace($"&PLACEHOLD{parsable}&",emoji.ToString());
                        }
                        return str;
                    }catch
                    {
                        return str;
                    }
                }
                return str;
            }
            return str;
        }
    }
}