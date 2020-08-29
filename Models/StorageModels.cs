using System;
using Newtonsoft.Json;
namespace homiebot 
{
    public class MemoryFile : StoredItem
    {
        [NonSerialized]
        private const string containerName = "RememberItems";
        public byte[] File;
        private static PartitionKey partKey = new PartitionKey(){
            KeyName = "Key",
            KeyPath = "/Key",
            KeyType = typeof(string)
        };
        public MemoryFile(string key) : base(key,containerName,partKey)
        {

        }
    }
    public class MemoryItem : StoredItem
    {
        [NonSerialized]
        private const string containerName = "RememberItems";
        public string User;
        public string Message;
        private static PartitionKey partKey = new PartitionKey(){
            KeyName = "Key",
            KeyPath = "/Key",
            KeyType = typeof(string)
        };
        public MemoryItem(string key) : base(key,containerName,partKey)
        {
            
        }
    }

    public class ReminderItem
    {
        [NonSerialized]
        public const string containerName = "Reminders";
        [NonSerialized]
        public static PartitionKey partKey = new PartitionKey(){
            KeyName = "User",
            KeyPath = "/User",
            KeyType = typeof(string)
        };
        public DateTime Time;
        public string Message;
        public string User;
        public ReminderItem(string user, DateTime time)
        {
            this.User = user;
            this.Time = time;
        }
    }

    public abstract class StoredItem
    {
        [NonSerialized]
        public string ContainerName;
        [NonSerialized]
        public PartitionKey PartitionKey;
        public string Key {get;set;}
        public string Owner;
        public StoredItem(string key, string containerName, PartitionKey partitionKey)
        {
            this.Key = key;
            ContainerName = containerName;
            this.PartitionKey = partitionKey;
        }
    }

    public class PartitionKey
    {
        public string KeyPath{get; set;}
        public string KeyName{get; set;}
        public object KeyValue{get;set;}
        public Type KeyType{get;set;}
    }
}