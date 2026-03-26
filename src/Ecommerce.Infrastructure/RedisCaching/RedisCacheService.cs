using Ecommerce.Application.Services.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ecommerce.Infrastructure.RedisCaching;

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
    private readonly IConnectionMultiplexer? _redis;
    private readonly IDatabase? _db;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly RedisCacheOptions _options;
    private readonly IMemoryCache _memoryCache;

    /// <param name="redis">
    /// Null khi <c>Caching:Provider=Memory</c> —
    /// chỉ dùng <see cref="IDistributedCache"/> (in-memory distributed).
    /// </param>
    public RedisCacheService(
        IDistributedCache distributedCache,
        IConnectionMultiplexer? redis,
        IMemoryCache memoryCache,
        ILogger<RedisCacheService> logger,
        IOptions<RedisCacheOptions> options)
    {
        _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        _memoryCache = memoryCache;
        _redis = redis;
        _db = _redis?.GetDatabase();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new RedisCacheOptions();
    }

    /// <summary>
    /// Redis multiplexer được cấu private bool IsRedisAvailable => _redis is not null && _redis.IsConnected;hình nhưng mất kết nối → fast-fail,
    /// tránh chờ timeout 5 s. Khi multiplexer null (Memory mode) thì false.
    /// </summary>
    private bool IsRedisAvailable => _redis is not null && _redis.IsConnected;

    public async Task<T?> GetAsync<T>(string key)
    {
        if (!IsRedisAvailable)
        {
            if (_memoryCache.TryGetValue(key, out T? localData))
            {
                _logger.LogInformation("Redis Down - L1 Cache Hit (RAM) for key: {CacheKey}", key);
                return localData;
            }
            _logger.LogWarning("Redis Down - L1 Cache Miss for key: {CacheKey}", key);
            return default;
        }

        try
        {
            var data = await _distributedCache.GetStringAsync(key);

            if (string.IsNullOrEmpty(data))
            {
               // _logger.LogDebug("Cache miss for key: {CacheKey}", key);
                return default;
            }

            //_logger.LogDebug("Cache hit for key: {CacheKey}", key);
            return JsonSerializer.Deserialize<T>(data, _options.JsonSerializerOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis Error. Fast fallback for key: {CacheKey}", key);
            return _memoryCache.Get<T>(key);
        }
    }
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        var expiry = expiration ?? TimeSpan.FromMinutes(_options.DefaultExpirationMinutes);

        if (value == null) return;

        try
        {

            _memoryCache.Set(key, value, expiry);

            if (!IsRedisAvailable)
            {
                _logger.LogWarning("Redis Down - Data saved to L1 (RAM) only. Key: {CacheKey}", key);
                return;
            }

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry
            };

            var serializedData = JsonSerializer.Serialize(value, _options.JsonSerializerOptions);
            await _distributedCache.SetStringAsync(key, serializedData, cacheOptions);

            _logger.LogDebug("Successfully set cache for key: {CacheKey}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sync L2 (Redis) for key: {CacheKey}. Data remains in L1.", key);
        }
    }


    public async Task RemoveAsync(string key)
    {
        _memoryCache.Remove(key);

        if (!IsRedisAvailable)
            return;

        try
        {
            await _distributedCache.RemoveAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error removing key: {CacheKey} from Redis.", key);
        }
    }

    public async Task<string> GetVersionAsync(string key)
    {
        if (_db is not null && IsRedisAvailable)
        {
            try
            {
                var val = await _db.StringGetAsync(key);
                if (!val.HasValue && val.IsNullOrEmpty)
                {
                    var redisVersion = val.ToString()!;
                    _memoryCache.Set(key, redisVersion, TimeSpan.FromMinutes(5));
                    return redisVersion;
                }    
                    
            }
            catch
            {
            }
        }
        if (_memoryCache.TryGetValue(key, out string? localVersion))
        {
            return localVersion ?? "1";
        }

        return "1";
    }

    public async Task<long> IncrementAsync(string key)
    {
        long newValue = 1;

        if (_db is not null && IsRedisAvailable)
        {
            try
            {
                newValue = await _db.StringIncrementAsync(key);
                // update ram
                _memoryCache.Set(key, newValue.ToString(), TimeSpan.FromMinutes(5));
                return newValue;
            }
            catch { /* if redis error*/ }
        }

        // 2. PHƯƠNG ÁN DỰ PHÒNG: Tự tăng trong RAM (Khi chạy Mode Memory hoặc Redis sập)
        if (_memoryCache.TryGetValue(key, out string? currentVal) && long.TryParse(currentVal, out long oldVal))
        {
            newValue = oldVal + 1;
        }
        else
        {
            newValue = 2; // Giả sử khởi tạo từ 1 lên 2
        }

        _memoryCache.Set(key, newValue.ToString(), TimeSpan.FromMinutes(5));
        return newValue;
    }
    public async Task RemoveByPrefixAsync(string prefix)
    {
        if (_redis is null || !_redis.IsConnected || _db is null)
            return;

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
                foreach (var k in keys)
                {
                    _memoryCache.Remove(k);
                    await _db.KeyDeleteAsync(k);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache pattern: {Prefix}", prefix);
        }
    }
}
