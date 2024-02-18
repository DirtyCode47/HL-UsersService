using Newtonsoft.Json;
using StackExchange.Redis;
using UsersService.Entities;

namespace UsersService.Cache
{
    public interface ICacheService
    {
        public void AddOrUpdateCache<T>(string key, T data);

        public T GetFromCache<T>(string key);

        public List<T> GetAllFromCache<T>(string pattern);
        public void ClearCache(string key);

        public void ClearAll();
    }
}
