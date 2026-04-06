namespace Ecommerce.Application.Services.Interfaces;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
    /// <summary>Đọc bản ghi phiên bản cache (Redis STRING). Soft-fail: lỗi Redis hoặc thiếu key → "1".</summary>
    Task<string> GetVersionAsync(string key);
    Task<long> IncrementAsync(string key);
    Task RemoveByPrefixAsync(string prefix); 
}
