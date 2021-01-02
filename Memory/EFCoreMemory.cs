using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
namespace homiebot.memory
{
    public class EFCoreMemory : IMemoryProvider
    {
        private HomiebotContext context;
        private ILogger logger;
        public EFCoreMemory(HomiebotContext context, ILogger<HomieBot> logger)
        {
            this.context = context;
            this.logger = logger;
        }
        void IMemoryProvider.StoreItem<T>(T item)
        {
            context.Add<T>(item);
            context.SaveChanges();
        }

        async Task IMemoryProvider.StoreItemAsync<T>(T item)
        {
            await context.AddAsync<T>(item);
            await context.SaveChangesAsync();
        }

        T IMemoryProvider.GetItem<T>(string id)
        {
            return getItem<T>(id);
        }

        async Task<T> IMemoryProvider.GetItemAsync<T>(string id)
        {
            return await getItemAsync<T>(id);
        }

        IEnumerable<T> IMemoryProvider.FindItems<T>(Func<T, bool> filter)
        {
            var dbset = getItemSet<T>();
            return dbset.Where<T>(filter);
        }

        IAsyncEnumerable<T> IMemoryProvider.FindAsyncItems<T>(Func<T, bool> filter)
        {
            var dbset = getItemSet<T>();
            return dbset.Where<T>(filter).AsQueryable<T>().AsAsyncEnumerable<T>();
        }

        DbSet<T> IMemoryProvider.GetItemSet<T>()
        {
            return getItemSet<T>();            
        }

        bool IMemoryProvider.RemoveItem<T>(T item)
        {
            context.Remove<T>(item);
            var changes = context.SaveChanges();
            if(changes != 0)
            {
                return true;
            }
            return false;
        }

        async Task<bool> IMemoryProvider.RemoveItemAsync<T>(T item)
        {
            context.Remove<T>(item);
            var changes = await context.SaveChangesAsync();
            if(changes != 0)
            {
                return true;
            }
            return false;
        }

        bool IMemoryProvider.RemoveItem<T>(string id)
        {
            T item = getItem<T>(id);
            context.Remove<T>(item);
            var changes = context.SaveChanges();
            if(changes != 0)
            {
                return true;
            }
            return false;
        }

        async Task<bool> IMemoryProvider.RemoveItemAsync<T>(string id)
        {
            T item = await getItemAsync<T>(id);
            context.Remove<T>(item);
            var changes = await context.SaveChangesAsync();
            if(changes != 0)
            {
                return true;
            }
            return false;
        }
        private DbSet<T> getItemSet<T>() where T: class
        {
            return context.Set<T>();
        }
        private T getItem<T>(string id) where T: class
        {
            try
            {
                T lookup = context.Find<T>(id);
                return lookup;
            }
            catch (Exception e)
            {
                logger.LogWarning(e,"Couldn't find item, might be a nonissue");
                return null;
            }
        }
        private async Task<T> getItemAsync<T>(string id) where T: class
        {
            try
            {
                T lookup = await context.FindAsync<T>(id);
                return lookup;
            }
            catch (Exception e)
            {
                logger.LogWarning(e,"Couldn't find item, might be a nonissue");
                return null;
            }
        }
    }
}