using System.Collections.Generic;

namespace Homiebot.Models
{
    public class BotConfig
    {
        public string DiscordToken {get; set;}
        public IEnumerable<string> CommandPrefixes{get;set;}
        public IEnumerable<string> Admins {get;set;}
        public bool UseVoice {get;set;}
        public bool UseBrain{get;set;}

        public string VoiceProvider {get;set;}
        public string ImageProvider {get;set;}
        public string ImageProcessor {get;set;}
        public string BrainProvider{get;set;}
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
    public class LocalFileStorageConfig 
    {
        public string FIleFormat {get;set;}
        public string StoragePath {get;set;}
    }
    public class ReactionConfig
    {
        public string ReactionName {get;set;}
        public string TriggerReaction {get;set;}
        public IEnumerable<string> Reactions {get;set;}
    }
    public class AWSConfig
    {
        public string AccessKey{get;set;}
        public string SecretKey{get;set;}
        public string Region {get;set;}
        public string BucketName {get;set;}
    }
    public class LocalImageStoreConfig
    {
        public string ImageStorePath {get;set;}
    }
}