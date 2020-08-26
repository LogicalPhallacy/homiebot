using System;
namespace homiebot 
{
    public class MemoryFile : StoredItem
    {
        private const string containerName = "RememberItems";
        public byte[] File;
        public MemoryFile(string key) : base(key,containerName)
        {

        }
    }
    public class MemoryItem : StoredItem
    {
        private const string containerName = "RememberItems";
        public string Message{get;set;}
        public MemoryItem(string key) : base(key,containerName)
        {
            
        }
    }

    public class ReminderItem : StoredItem
    {
        private const string containerName = "ReminderItems";
        public string User {get; set;}
        public DateTime Time {get; set;}
        public string Message {get; set;}
        public ReminderItem(string user, DateTime time) : base($"{user}-{time.ToString()}", containerName)
        {
        }
    }

    public abstract class StoredItem
    {
        private string key;
        private string containerName;
        public string Key 
        {
            get => key;
        }
        public StoredItem(string key, string containerName)
        {
            this.key = key;
            this.containerName = containerName;
        }
    }
}