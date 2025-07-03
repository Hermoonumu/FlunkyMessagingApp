using System.Security.Claims;
using MessagingApp.DTO;
using MessagingApp.Models;
using Microsoft.IdentityModel.Tokens;

namespace MessagingApp.Services;


public interface IAuthService
{
    public ClaimsIdentity getClaims(User user);
    public Task<Dictionary<string, string>> GenerateTokensAsync(User user);
    public Task<TokenDTO> VerifyTokenAsync(string token);
    public Task<User> UserByJWTAsync(HttpContext context);
    public Task<string> ExtractTokenFromHeaderAsync(HttpContext context);
    public Task RevokeTokensAsync(string token);
    public Task<Dictionary<string, string>> RefreshTokensAsync(string token);
}