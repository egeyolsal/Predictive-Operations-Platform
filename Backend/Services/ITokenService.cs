using TaskInventoryApi.Models;

namespace TaskInventoryApi.Services;

public interface ITokenService
{
    string CreateToken(User user);
}