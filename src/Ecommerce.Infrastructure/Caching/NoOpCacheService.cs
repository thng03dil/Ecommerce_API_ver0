using Ecommerce.Application.Services.Interfaces;

namespace Ecommerce.Infrastructure.Caching;

/// <summary>
/// Không cache — luôn miss. Dùng khi <c>Caching:Provider=None</c> (Docker, direct-DB mode).
/// Tất cả thao tác đọc trả về default/null/"1"; thao tác ghi/xóa là no-op.
/// </summary>
public sealed class NoOpCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key) => Task.FromResult<T?>(default);

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) => Task.CompletedTask;

    public Task RemoveAsync(string key) => Task.CompletedTask;

    public Task<string> GetVersionAsync(string key) => Task.FromResult("1");

    public Task<long> IncrementAsync(string key) => Task.FromResult(1L);

    public Task RemoveByPrefixAsync(string prefix) => Task.CompletedTask;
}
