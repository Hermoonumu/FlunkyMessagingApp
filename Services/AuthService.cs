using System.IdentityModel.Tokens.Jwt;
using System.Text;
using MessagingApp.Models;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace MessagingApp.Services;


public class AuthService : ServiceCollection
{
    private ClaimsIdentity getClaims(User user)
    {
        ClaimsIdentity claims = new ClaimsIdentity();
        claims.AddClaim(new Claim(ClaimTypes.Name, user.Username));
        claims.AddClaim(new Claim(ClaimTypes.Role, user.Role.ToString()));
        return claims;
    }

    public string GenerateToken(User user)
    {
        JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
        byte[] key = Encoding.ASCII.GetBytes(MessagingApp.Settings.PrivateKey);
        SigningCredentials credentials = new SigningCredentials
            (
                new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha384Signature
            );

        SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = getClaims(user),
            Issuer = MessagingApp.Settings.Issuer,
            Expires = DateTime.Now.AddMinutes(30),
            SigningCredentials = credentials
        };
        return handler.WriteToken(handler.CreateToken(tokenDescriptor));
    }
}