using System.IdentityModel.Tokens.Jwt;
using System.Text;
using MessagingApp.Models;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using MessagingApp.DTO;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;

namespace MessagingApp.Services;


public class AuthService(DataContext _db, UserService _userSvc) : ServiceCollection
{
    private ClaimsIdentity getClaims(User user)
    {
        ClaimsIdentity claims = new ClaimsIdentity();
        claims.AddClaim(new Claim(ClaimTypes.Name, user.Username!));
        claims.AddClaim(new Claim(ClaimTypes.Role, user.Role.ToString()!));
        claims.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.ID.ToString()!));
        return claims;
    }

    public async Task<Dictionary<string, string>> GenerateTokens(User user)
    {
        Dictionary<string, string> tokens = new Dictionary<string, string>();
        JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
        byte[] key = Encoding.ASCII.GetBytes(MessagingApp.Settings.PrivateKey);
        SigningCredentials credentials = new SigningCredentials
            (
                new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha512Signature
            );

        SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = getClaims(user),
            Issuer = MessagingApp.Settings.Issuer,
            Audience = MessagingApp.Settings.Audience,
            Expires = DateTime.Now.AddMinutes(30),
            SigningCredentials = credentials
        };
        tokens.Add("accessToken", handler.WriteToken(handler.CreateToken(tokenDescriptor)));
        byte[] randStringToken = new byte[128];
        RandomNumberGenerator.Create().GetBytes(randStringToken);
        tokens.Add("refreshToken", Convert.ToBase64String(randStringToken)!);

        List<RefreshToken> tokensBelongingToUser = _db.RefreshTokens.Where(tk => tk.UserID == user.ID).ToList();
        foreach (RefreshToken i in tokensBelongingToUser)
        {
            _db.RefreshTokens.Remove(i);
        }

        _db.RefreshTokens.Add(new RefreshToken()
        {
            UserID = (long)user.ID!,
            user = user,
            Token = tokens["refreshToken"],
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow + Settings.RefreshTokenValidTerm,
            isRevoked = false
        });

        await _db.SaveChangesAsync();
        return tokens;
    }


    public async Task<ClaimsPrincipal> VerifyToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var validationResult = handler.ValidateToken(token,
        new TokenValidationParameters()
        {
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Settings.PrivateKey)),
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidIssuer = Settings.Issuer,
            ValidateAudience = true,
            ValidAudience = Settings.Audience,
            ValidateLifetime = true
        }, out SecurityToken validatedToken);
        return validationResult;
    }


    public async Task<User> UserByJWTAsync(HttpContext context)
    {
        string[] token = context.Request.Headers["Authorization"].ToString().Split(' ');
        if (token.Count() < 2) return null;
        if (token[0] != "Bearer") return null;
        ClaimsPrincipal decodedToken;
        try
        {
            decodedToken = await VerifyToken(token[1]);
        }
        catch (SecurityTokenExpiredException)
        {
            return null;
        }
        if (decodedToken.Identity!.IsAuthenticated == false)
        {
            return null;
        }
        string username = decodedToken.Claims.Select(c => c.Value).ToList()[0];
        return await _userSvc.GetUser(new AuthDTO() { Username = username });
    }
}