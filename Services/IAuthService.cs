using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MessagingApp.Models;
using static MessagingApp.Services.Implementation.AuthService;

namespace MessagingApp.Services;


public interface IAuthService
{
    public ClaimsIdentity createClaims(User user);
    public Task<Dictionary<string, string>> GenerateTokensAsync(User user, bool expired = false);
    public (ClaimsPrincipal? Principal, JwtSecurityToken? Jwt) ValidateToken(string token);
    public Task<User> UserByJWTAsync(HttpContext context);
    public Task<string> ExtractTokenFromHeaderAsync(HttpContext context);
    public Task RevokeTokensAsync(string token);
    public Task<Dictionary<string, string>> RefreshTokensAsync(string token);
}