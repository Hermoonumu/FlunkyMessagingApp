using System.Security.Claims;
using MessagingApp.Models;

namespace MessagingApp.Services;


public interface IAuthService
{
    public ClaimsIdentity getClaims(User user);
    public Task<Dictionary<string, string>> GenerateTokensAsync(User user);
    public Task<ClaimsPrincipal> VerifyTokenAsync(string token);
    public Task<User> UserByJWTAsync(HttpContext context);
}