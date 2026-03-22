using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Application.Services.Interfaces
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task RemoveAsync(string key);
        /// <summary>Đọc bản ghi phiên bản cache (Redis STRING). Soft-fail: lỗi Redis hoặc thiếu key → "1".</summary>
        Task<string> GetVersionAsync(string key);
        Task<long> IncrementAsync(string key);
        Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
        Task RemoveByPrefixAsync(string prefix);
    }
}
