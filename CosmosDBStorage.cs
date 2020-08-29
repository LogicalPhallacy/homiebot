using System.Threading.Tasks;
using System.Linq;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Microsoft.Azure.Cosmos.Linq;
using System;

namespace homiebot 
{
    public class CosmosStorage : IPersistantStorage
    {
        public bool Connected => throw new System.NotImplementedException();

        private readonly ILogger logger;
        private readonly IConfiguration configuration;
        private CosmosDbConfig dbConfig;
        private CosmosClient cosmosClient;
        private Database defaultDB;

        public CosmosStorage(ILogger<HomieBot> logger, IConfiguration configuration)
        {
            this.logger = logger;
            this.configuration = configuration;
            dbConfig = configuration.GetSection("CosmosStorageConfig").Get<CosmosDbConfig>();
        }

        public async Task AddStoredItem<T>(string store, PartitionKey partitionKey, T value, bool canUpsert = false)
        {
            var container = await defaultDB.CreateContainerIfNotExistsAsync(store,partitionKey.KeyPath);
            if(container.Container != null)
            {
                var c = container.Container;
                if(canUpsert)
                {
                    if((await c.UpsertItemAsync<T>(value)).StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        logger.LogInformation("Stored item successfully");
                    }
                }
                else
                {
                    T ExistingItem;
                    try{
                        ExistingItem = await this.GetStoredItem<T>(store,partitionKey);
                    }
                    catch(MissingItemException mi){
                        logger.LogInformation("Item doesn't exist, adding new");
                        if((await c.CreateItemAsync<T>(value,new Microsoft.Azure.Cosmos.PartitionKey())).StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            logger.LogInformation("Stored item successfully");
                        }
                    }
                    catch(Exception e){
                        logger.LogError(e,"Error with Cosmos");
                    }
                }
            }else{
                logger.LogError("Error with Cosmos");
            }
        }

        public async Task Connect()
        {
            cosmosClient = new CosmosClient(dbConfig.EndPointURI,dbConfig.PrimaryKey);
            defaultDB = await cosmosClient.CreateDatabaseIfNotExistsAsync(dbConfig.DefaultDatabase);
        }

        public async Task<T> GetStoredItem<T>(string store, PartitionKey partitionKey)
        {
            Container c = defaultDB.GetContainer(store);
            FeedIterator<T> feed = c.GetItemLinqQueryable<T>().Where(item=>partitionKey.KeyType.GetProperty(partitionKey.KeyName).GetValue(item) == partitionKey.KeyValue).ToFeedIterator<T>();
            while(feed.HasMoreResults){
                var resp = await feed.ReadNextAsync();
                return resp.FirstOrDefault();
            }
            throw new System.Exception("Couldn't find item with that key");
        }
    }

    public class CosmosDbConfig
    {
        public string DefaultDatabase{get;set;}
        public string EndPointURI{get;set;}
        public string PrimaryKey{get;set;}
    }
    [System.Serializable]
    public class MissingItemException : System.Exception
    {
        public MissingItemException() { }
        public MissingItemException(string message) : base(message) { }
        public MissingItemException(string message, System.Exception inner) : base(message, inner) { }
        protected MissingItemException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}