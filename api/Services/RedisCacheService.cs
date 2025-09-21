using System;
using api.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace api.Services
{
    public class RedisCacheService : IRedisCacheService
    {
        private readonly IDistributedCache _cache;

        public RedisCacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public T? GetData<T>(string key)
        {
            try
            {
                var data = _cache.GetString(key);
                return data == null ? default : JsonConvert.DeserializeObject<T>(data);
            }
            catch
            {

                return default;
            }
        }

        public void SetData<T>(string key, T value, TimeSpan absoluteExpireTime)
        {
            try
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = absoluteExpireTime
                };

                var data = JsonConvert.SerializeObject(value);
                _cache.SetString(key, data, options);
            }
            catch
            {

            }
        }

        public void SetData<T>(string key, T value)
        {
            try
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(45)
                };

                var data = JsonConvert.SerializeObject(value);
                _cache.SetString(key, data, options);
            }
            catch
            {

            }
        }

        public void RemoveData(string key)
        {
            try
            {
                _cache.Remove(key);
            }
            catch
            {

            }
        }
    }
}
