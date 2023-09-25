using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeploymentRobotService.MyHelper.Cache
{
    public class FsInteractiveUpdateCache
    {
        private static MemoryCache GetUserRulesMemoryCache { get; set; }

        private static MemoryCacheEntryOptions memoryCacheEntryOptions;

        static FsInteractiveUpdateCache()
        {
            GetUserRulesMemoryCache = new MemoryCache(new MemoryCacheOptions() { SizeLimit = 500, CompactionPercentage = 0.3, ExpirationScanFrequency = TimeSpan.FromSeconds(3600) });
            memoryCacheEntryOptions = new MemoryCacheEntryOptions() { Size = 1, SlidingExpiration = TimeSpan.FromDays(30) };
        }

        public static void AddCache(string key, string messageId)
        {
            GetUserRulesMemoryCache.Set<string>(key, messageId, memoryCacheEntryOptions);
        }

        public static string GetCache(string key)
        {
            return GetUserRulesMemoryCache.Get<string>(key);
        }

        public static void RemoveCache(string key)
        {
            GetUserRulesMemoryCache.Remove(key);
        }
    }
}
