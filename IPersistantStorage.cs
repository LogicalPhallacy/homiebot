using System.Threading.Tasks;

namespace homiebot 
{
    public interface IPersistantStorage
    {
        public Task AddStoredItem<T>(string store, PartitionKey partitionKey, T value, bool canUpsert = false);
        public Task<T> GetStoredItem<T>(string store, PartitionKey partitionKey);
        public bool Connected{get;}
        public Task Connect();
    }
}