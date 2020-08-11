using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;
namespace homiebot
{
    public class ChatReplacer
    {
        private IEnumerable<Gimmick> Gimmicks;
        public IEnumerable<string> Commands => Gimmicks.Select(g => g.Command);
        public ChatReplacer()
        {
            
        }

        public void Initialize()
        {
            // Load Gimmicks here
            var Gimmickfile = JsonSerializer.Deserialize<GimmickFile>(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"ChatGimmicks.json")));
            Gimmicks = Gimmickfile.Gimmicks;
        }
        public string Replace(string command, params string[] args)
        {
            var gimmick = Gimmicks.Where<Gimmick>(g => g.Command.ToLower() == command).FirstOrDefault();
            if(gimmick == null)
            {
                return "Homie don't play that.";
            }
            string retstr = gimmick.ReplacementStrings.OrderBy(x => Program.random.Next()).First();
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
    }

    public class GimmickFile 
    {
        public IEnumerable<Gimmick> Gimmicks {get; set;}
    }
    public class Gimmick
    {
        public string Command {get; set;}
        public IEnumerable<string> ReplacementStrings {get; set;}
        public string ArgSplitter {get;set;}
        public int ArgCount {get;set;}
    }
}