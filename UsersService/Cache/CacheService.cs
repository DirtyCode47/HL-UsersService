
using Newtonsoft.Json;
using UsersService.Entities;
using StackExchange.Redis;

namespace UsersService.Cache
{
    public class CacheService
    {
        private readonly IConnectionMultiplexer _redisConnection;

        public CacheService(IConnectionMultiplexer redisConnection)
        {
            _redisConnection = redisConnection ?? throw new ArgumentNullException(nameof(redisConnection));
        }

        public void AddOrUpdateCache<T>(string key, T data)
        {
            var database = _redisConnection.GetDatabase();
            var serializedData = JsonConvert.SerializeObject(data);
            database.StringSet(key, serializedData);
        }

        public T GetFromCache<T>(string key)
        {
            var database = _redisConnection.GetDatabase();
            var serializedData = database.StringGet(key);
            if (serializedData.HasValue)
            {
                return JsonConvert.DeserializeObject<T>(serializedData);
            }
            return default;
        }

        public List<T> GetAllFromCache<T>(string pattern)
        {
            var database = _redisConnection.GetDatabase();
            var keys = database.Multiplexer.GetServer(_redisConnection.GetEndPoints()[0]).Keys(pattern: pattern);
            var result = new List<T>();

            foreach (var key in keys)
            {
                var serializedData = database.StringGet(key);
                if (serializedData.HasValue)
                {
                    var deserializedData = JsonConvert.DeserializeObject<T>(serializedData);
                    result.Add(deserializedData);
                }
            }

            return result;
        }

        public void ClearCache(string key)
        {
            var database = _redisConnection.GetDatabase();
            database.KeyDelete(key);
        }

        public void ClearAll()
        {
            var server = _redisConnection.GetServer(_redisConnection.GetEndPoints()[0]);
            server.FlushAllDatabases();
        }

        public void InitializeCache(List<User> users)
        {
            var database = _redisConnection.GetDatabase();

            // Очищаем все данные в кэше перед инициализацией
            ClearAll();

            foreach (var post in users)
            {
                var cacheKey = $"post:{post.id}";
                var serializedPost = JsonConvert.SerializeObject(post);
                database.StringSet(cacheKey, serializedPost);
            }
        }
    }
}