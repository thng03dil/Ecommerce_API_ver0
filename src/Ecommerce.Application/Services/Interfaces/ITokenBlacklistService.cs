namespace Ecommerce.Application.Services.Interfaces
{
    public interface ITokenBlacklistService
    {
        Task<bool> IsBlacklistedAsync(string jtiHash, CancellationToken cancellationToken = default);
        Task BlacklistAsync(string jtiHash, TimeSpan ttl, CancellationToken cancellationToken = default);
    }
}
