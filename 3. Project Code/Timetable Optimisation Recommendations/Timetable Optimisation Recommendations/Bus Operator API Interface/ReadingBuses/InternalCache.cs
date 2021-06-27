// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using System.Collections.Concurrent;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;

namespace Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface.ReadingBuses
{
    /// <summary>
    /// The Internal Cache class is used to store objects of type T, into program memory
    /// this is done for the best possible performance, while also ensuring that we don't
    /// exhaust all of a devices memory.
    /// </summary>
    /// <typeparam name="T">Any type T for the cache object.</typeparam>
    internal class InternalCache<T>
    {
        /// <summary>
        /// Actually stores the objects into the default cache memory.
        /// This is shared between all cache objects of any type. 
        /// </summary>
        private readonly MemoryCache _cache =  MemoryCache.Default;

        /// <summary>
        /// A concurrent dictionary of semaphores, the key is the cache key and this is used to
        /// ensure that two or more threads are not modifying the cache at the same time.
        /// </summary>
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();



        /// <summary>
        /// Gets the item from cache, or if it doesn't yet exist in cache memory "create" the object
        /// store it into cache and then return it.
        /// </summary>
        /// <param name="key">The unique ID for the cache record.</param>
        /// <param name="createItem">A function pointer to a method to create the object if it doesn't yet exist.</param>
        /// <returns></returns>
        public async Task<T?> GetOrCreate(string key, Func<Task<T?>> createItem)
        {
            //Tries to get the value from cache.
            CacheItem? cacheRecord = _cache.GetCacheItem(key);
     
            //If it doesn't exist in the cache then create it.
            if (cacheRecord == null)
            {
                //Stops any other thread from creating the item at the same time.
                SemaphoreSlim mylock = _locks.GetOrAdd(key, k => new SemaphoreSlim(1, 1));
                T? cacheObject;
                await mylock.WaitAsync();
                try
                {
                    //Creates the item
                    cacheObject = await createItem();
                    //After 10 min if it hasn't been used delete from cache automatically.
                    CacheItemPolicy policy = new() { AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(10.0) };
                    //If the item was created successfully then store it into cache.
                    if(cacheObject != null)
                        _cache.Set(key, cacheObject, policy);  
                }
                finally
                {
                    //Let another thread edit the cache.
                    mylock.Release();
                }
                //Return the cached object.
                return cacheObject;
            }
            
            //Else it did exist in cache so get the value and cast to it, returning it.
            return (T)cacheRecord.Value;
        }
    }
}

