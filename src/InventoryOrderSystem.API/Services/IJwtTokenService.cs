using InventoryOrderSystem.Domain.Entities;

namespace InventoryOrderSystem.API.Services;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAtUtc) GenerateToken(ApplicationUser user, IList<string> roles);
}
