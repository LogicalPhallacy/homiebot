using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Homiebot.Brain
{
    public interface IMemoryProvider : IDisposable
    {
        void StoreItem<T> (T item) where T : class, IMemorableObject ;
        Task StoreItemAsync<T>(T item) where T : class, IMemorableObject;

        T GetItem<T>(string id) where T : class, IMemorableObject;
        Task<T> GetItemAsync<T>(string id) where T : class, IMemorableObject;

        bool RemoveItem<T>(string id) where T : class, IMemorableObject;
        Task<bool> RemoveItemAsync<T>(string id) where T : class, IMemorableObject;
        bool RemoveItem<T>(T item) where T : class, IMemorableObject;
        Task<bool> RemoveItemAsync<T>(T item) where T : class, IMemorableObject;

        IEnumerable<T> FindItems<T>(Func<T, bool> filter) where T : class, IMemorableObject;
        IAsyncEnumerable<T> FindAsyncItems<T>(Func<T, bool> filter) where T : class, IMemorableObject;

        DbSet<T> GetItemSet<T>() where T : class, IMemorableObject;
    }
}