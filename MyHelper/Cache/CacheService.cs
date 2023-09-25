using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeHttpWebService.Services.Cache
{
    public class CacheService
    {
        public static MemoryCache ControllerApiMemoryCache { get; private set; }

        private static MemoryCache DefaultCustomMemoryCache { get; set; }

        private static MemoryCacheEntryOptions DefaultCustomMemoryCacheEntryOptions { get; set; }

        static CacheService()
        {
            ControllerApiMemoryCache = new MemoryCache(new MemoryCacheOptions() { SizeLimit = 100, CompactionPercentage = 0.3, ExpirationScanFrequency = TimeSpan.FromSeconds(60) }); //MemoryCacheEntryOptions  Size 可以设置为容量 或 1
            DefaultCustomMemoryCache = new MemoryCache(new MemoryCacheOptions() { SizeLimit = 500, CompactionPercentage = 0.3, ExpirationScanFrequency = TimeSpan.FromSeconds(60) });
            DefaultCustomMemoryCacheEntryOptions = new MemoryCacheEntryOptions() { Size = 1, SlidingExpiration = TimeSpan.FromDays(100) };
        }

        public void Add()
        {

        }


        private static string GetFuncParameterKey(object obj)
        {
            if (obj is object[])
            {
                return GetFuncParameterKey(obj as object[]);
            }
            else
            {
                return obj?.ToString() ?? "";
            }
        }

        private static string GetFuncParameterKey(object[] ags)
        {
            if (ags != null && ags.Length > 0)
            {
                string key = null;
                if (ags.Length < 3)
                {
                    key = ags[0]?.ToString() ?? "";
                    if (ags.Length == 2)
                    {
                        key += $"{Environment.NewLine}{ ags[1]?.ToString() ?? ""}";
                    }
                }
                else
                {
                    StringBuilder sbKey = new StringBuilder();
                    foreach (var item in ags)
                    {
                        sbKey.AppendLine(item?.ToString() ?? "");
                    }
                    key = sbKey.ToString();
                }
                return key;
            }
            return default;
        }

        private static string GetMethodPathStr(int frame = 1)
        {
            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace(true);
            System.Reflection.MethodBase methodBase = stackTrace.GetFrame(frame).GetMethod();
            //return $"[{methodBase.DeclaringType.Namespace}/{methodBase.DeclaringType.FullName}/{methodBase.Name}]";
            return $"[{methodBase.DeclaringType.FullName}]";
        }

        public static object GetCustomCache(object parameters, MemoryCache nowMemoryCache = null)
        {
            string invokMethod = null;
            if (nowMemoryCache == null)
            {
                nowMemoryCache = DefaultCustomMemoryCache;
                invokMethod = GetMethodPathStr(2);
            }
            string key = GetFuncParameterKey(parameters);
            if (key != null)
            {
                return nowMemoryCache.Get(invokMethod == null ? key : $"{invokMethod}{Environment.NewLine}{key}");
            }
            else
            {
                throw new ArgumentException("parameters ags is error");
            }
        }

        public static void SetCustomCache(object cacheObject, object parameters, MemoryCache nowMemoryCache = null)
        {
            string invokMethod = null;
            if (nowMemoryCache == null)
            {
                nowMemoryCache = DefaultCustomMemoryCache;
                invokMethod = GetMethodPathStr(2);
            }
            if (cacheObject == null)
            {
                throw new ArgumentException("object cacheObject is error");
            }
            string key = GetFuncParameterKey(parameters);
            if (key != null)
            {
                //memoryCacheEntryOptions = new MemoryCacheEntryOptions() { Size = 1, AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheMinutes) };
                nowMemoryCache.Set(invokMethod == null ? key : $"{invokMethod}{Environment.NewLine}{key}", cacheObject, DefaultCustomMemoryCacheEntryOptions);
            }
            else
            {
                throw new ArgumentException("parameters ags is error");
            }
        }

    }
}
