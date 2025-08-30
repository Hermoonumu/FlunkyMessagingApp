using System.IdentityModel.Tokens.Jwt;
using System.Text;
using MessagingApp.Models;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using MessagingApp.DTO;
using Microsoft.EntityFrameworkCore;
namespace MessagingApp.Services.Implementation;


public class AuthService(DataContext _db,
                            IUserService _userSvc,
                            IConfiguration _conf) : IAuthService
{
    public ClaimsIdentity getClaims(User user)
    {
        ClaimsIdentity claims = new ClaimsIdentity();
        claims.AddClaim(new Claim(ClaimTypes.Name, user.Username!));
        claims.AddClaim(new Claim(ClaimTypes.Role, user.Role.ToString()!));
        claims.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.ID.ToString()!));
        claims.AddClaim(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
        return claims;
    }

    public async Task<Dictionary<string, string>> GenerateTokensAsync(User user, bool expired = false)
    {
        Dictionary<string, string> tokens = new Dictionary<string, string>();
        JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
        byte[] key = Encoding.ASCII.GetBytes(_conf.GetSection("SecConfig").GetValue<String>("PrivateKey")!);
        SigningCredentials credentials = new SigningCredentials
            (
                new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha512Signature
            );

        SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = getClaims(user),
            Issuer = _conf.GetSection("SecConfig").GetValue<String>("Issuer"),
            Audience = _conf.GetSection("SecConfig").GetValue<String>("Audience"),
            Expires = DateTime.Now.Add(expired?TimeSpan.FromSeconds(0):TimeSpan.FromMinutes(15)),
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
            ExpiresAt = DateTime.UtcNow + TimeSpan.FromDays(_conf.GetSection("SecConfig").GetValue<int>("DurationDays"))
        });

        await _db.SaveChangesAsync();
        return tokens;
    }


    public async Task<TokenDTO?> VerifyTokenAsync(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        ClaimsPrincipal claims;
        SecurityToken secToken;
        try
        {
            claims = handler.ValidateToken(token,
            new TokenValidationParameters()
            {
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(
                    _conf.GetSection("SecConfig").GetValue<String>("PrivateKey")!
                    )),
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidIssuer = _conf.GetSection("SecConfig").GetValue<String>("Issuer")!,
                ValidateAudience = true,
                ValidAudience = _conf.GetSection("SecConfig").GetValue<String>("Audience")!,
                ValidateLifetime = true
            }, out secToken);
        }
        catch (Exception)
        {
            return null;
        }
        return new TokenDTO() { claims = claims, SecToken = secToken };
    }


    public async Task<User?> UserByJWTAsync(HttpContext context)
    {
        ClaimsIdentity decodedToken;
        decodedToken = (context.User.Identity as ClaimsIdentity)!;
        string username = decodedToken.Claims.Select(c => c.Value).ToList()[0];
        return await _userSvc.GetUserAsync(new AuthDTO() { Username = username }, false);
    }

    public async Task<string> ExtractTokenFromHeaderAsync(HttpContext context)
    {
        string[] token = context.Request.Headers["Authorization"].ToString().Split(' ');
        return token[1];
    }

    public async Task RevokeTokensAsync(string token)
    {
        TokenDTO tokenInfo = (await VerifyTokenAsync(token))!;
        await _db.revokedJWTs.AddAsync(new RevokedJWTs() { Token = token, JTI = tokenInfo.SecToken.Id });
        _db.SaveChanges();
        long userID = long.Parse(tokenInfo.claims.Claims.Select(c => c.Value).ToList()[2]);
        RefreshToken? refreshToken = await _db.RefreshTokens.FirstOrDefaultAsync(tk => tk.UserID == userID);
        if (refreshToken != null)
        {
            _db.RefreshTokens.Remove(refreshToken);
            _db.SaveChanges();
        }
    }




    public async Task<Dictionary<string, string>?> RefreshTokensAsync(string refreshToken)
    {
        RefreshToken? token = await _db.RefreshTokens.FirstOrDefaultAsync(tk => tk.Token == refreshToken);
        if (token == null) { return null; }
        User user = (await _db.Users.FirstOrDefaultAsync(u => u.ID == token.UserID))!;
        return await GenerateTokensAsync(user);
    }
}