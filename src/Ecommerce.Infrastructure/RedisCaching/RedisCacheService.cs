using Ecommerce.Application.Services.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ecommerce.Infrastructure.RedisCaching
{
    public class RedisCacheOptions
    {
        public JsonSerializerOptions JsonSerializerOptions { get; set; } = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        public int DefaultExpirationMinutes { get; set; } = 10;

        /// <summary>
        /// Phải khớp InstanceName của AddStackExchangeRedisCache để pattern xóa trùng key vật lý (vd. Ecommerce:auth:session:...).
        /// </summary>
        public string DistributedCacheKeyPrefix { get; set; } = string.Empty;
    }

    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;   // tác dụng trong đếm số lượt xem sản phẩm
        private readonly ILogger<RedisCacheService> _logger;
        private readonly RedisCacheOptions _options;

        public RedisCacheService(
            IDistributedCache distributedCache,
            IConnectionMultiplexer redis,
            ILogger<RedisCacheService> logger,
            IOptions<RedisCacheOptions> options)
        {
            _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            _db = _redis.GetDatabase();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Lấy options hoặc dùng mặc định nếu chưa đăng ký trong Program.cs
            _options = options?.Value ?? new RedisCacheOptions();
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var data = await _distributedCache.GetStringAsync(key);

                if (string.IsNullOrEmpty(data))
                {
                    _logger.LogDebug("Cache miss for key: {CacheKey}", key);
                    return default;
                }

                _logger.LogDebug("Cache hit for key: {CacheKey}", key);
                return JsonSerializer.Deserialize<T>(data, _options.JsonSerializerOptions);
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Redis unavailable. Fallback to DB for key: {CacheKey}", key);
                return default;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis error reading key: {CacheKey}. Fallback to DB.", key);
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(_options.DefaultExpirationMinutes)
                };

                var serializedData = JsonSerializer.Serialize(value, _options.JsonSerializerOptions);
                await _distributedCache.SetStringAsync(key, serializedData, cacheOptions);

                _logger.LogDebug("Successfully set cache for key: {CacheKey}", key);
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Redis unavailable. Skip cache write for key: {CacheKey}", key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis error writing key: {CacheKey}. Skip cache.", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _distributedCache.RemoveAsync(key);
                _logger.LogDebug("Successfully removed cache for key: {CacheKey}", key);
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Redis unavailable. Skip cache remove for key: {CacheKey}", key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis error removing key: {CacheKey}. Skip.", key);
            }
        }

        public async Task<string> GetVersionAsync(string key)
        {
            try
            {
                var val = await _db.StringGetAsync(key);
                if (!val.HasValue || val.IsNullOrEmpty)
                    return "1";

                return val.ToString()!;
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Redis unavailable. Fallback version '1' for key: {CacheKey}", key);
                return "1";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis error reading version for key: {CacheKey}. Fallback to '1'.", key);
                return "1";
            }
        }

        public async Task<long> IncrementAsync(string key)
        {
            try
            {
                var newValue = await _db.StringIncrementAsync(key);
                _logger.LogDebug("Successfully incremented cache for key: {CacheKey} to {Value}", key, newValue);
                return newValue;
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Redis unavailable. Increment soft-fail for key: {CacheKey}", key);
                return 1L;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis error incrementing key: {CacheKey}. Soft-fail return 1.", key);
                return 1L;
            }
        }

        public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            var cachedValue = await GetAsync<T>(key);

            if (cachedValue != null && !cachedValue.Equals(default(T)))
                return cachedValue;

            var value = await factory();

            if (value != null)
                await SetAsync(key, value, expiration);

            return value;
        }
        public async Task RemoveByPrefixAsync(string prefix)
        {
            try
            {
                var physicalPrefix = string.IsNullOrEmpty(_options.DistributedCacheKeyPrefix)
                    ? prefix
                    : $"{_options.DistributedCacheKeyPrefix}{prefix}";
                var pattern = $"{physicalPrefix}*";

                var endpoints = _redis.GetEndPoints();
                foreach (var endpoint in endpoints)
                {
                    var server = _redis.GetServer(endpoint);
                    var keys = server.Keys(database: _db.Database, pattern: pattern).ToArray();
                    foreach (var key in keys)
                        await _db.KeyDeleteAsync(key);
                }
                _logger.LogInformation("Removed cache pattern: {Pattern}", pattern);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache pattern: {Prefix}", prefix);
            }
        }
    }
}
