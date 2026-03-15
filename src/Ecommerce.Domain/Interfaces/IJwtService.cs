using Ecommerce.Domain.Entities;
using System.Security.Claims;

namespace Ecommerce.Domain.Interfaces
{
    public interface IJwtService
    {
        string GenerateAccessToken(User user);

        string GenerateRefreshToken();

        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }
}
  