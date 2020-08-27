using System.Collections.Generic;

namespace homiebot
{
    public class BotConfig
    {
        public string DiscordToken {get; set;}
        public IEnumerable<string> CommandPrefixes{get;set;}
    }
    public class GimmickFile 
    {
        public IEnumerable<Gimmick> Gimmicks {get; set;}
    }
    public class CosmosStorageConfig
    {
        public string EndPoint {get;set;}
        public string ConnectionKey {get;set;}
        public string DatabaseName {get;set;}
    }
    public class ReactionConfig
    {
        public string ReactionName {get;set;}
        public string TriggerReaction {get;set;}
        public IEnumerable<string> Reactions {get;set;}
    }
}