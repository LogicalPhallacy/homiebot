using System;
using System.ComponentModel.DataAnnotations;
namespace Homiebot.Brain 
{
    public class MemoryFile : StoredItem
    {
        private const string containerName = "RememberItems";
        public byte[] File {get;set;}
        public MemoryFile(string key) : base(key,containerName)
        {

        }
        public MemoryFile() 
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
        public MemoryItem()
        {

        }
    }

    public class ReminderItem : StoredItem
    {
        public ReminderItem()
        {

        }
        private const string containerName = "ReminderItems";
        public string User {get; set;}
        public DateTime Time {get; set;}
        public string Message {get; set;}
        public ReminderItem(string user, DateTime time) : base($"{user}-{time.ToString()}", containerName)
        {
        }
    }

    public abstract class StoredItem : IMemorableObject
    {
        internal StoredItem()
        {

        }
        private string key;
        private string containerName;
        
        [Key]
        public string Key 
        {
            get => key;
            private set => key = value;
        }

        public string GuildName{get;set;}
        public string Owner{get;set;}
        public Type idType => key.GetType();

        object IMemorableObject.Id { 
            get => key; 
            set => key=(string)value;
            }

        public StoredItem(string key, string containerName)
        {
            this.key = key;
            this.containerName = containerName;
        }
    }
}