using System.IdentityModel.Tokens.Jwt;
using System.Text;
using MessagingApp.Models;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using MessagingApp.DTO;
namespace MessagingApp.Services.Implementation;


public class AuthService(   DataContext _db,
                            IUserService _userSvc,
                            IConfiguration _conf,
                            ILoggerService _logSvc) : IAuthService
{
    public ClaimsIdentity getClaims(User user)
    {
        _logSvc.LogInfo($"Generating token claims for {user.Username}", "AUTHClaimGen");
        ClaimsIdentity claims = new ClaimsIdentity();
        claims.AddClaim(new Claim(ClaimTypes.Name, user.Username!));
        claims.AddClaim(new Claim(ClaimTypes.Role, user.Role.ToString()!));
        claims.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.ID.ToString()!));
        return claims;
    }

    public async Task<Dictionary<string, string>> GenerateTokensAsync(User user)
    {
        await _logSvc.LogInfo($"Generating tokenpair for {user.Username}", "AUTHTokenGen");
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
            Expires = DateTime.Now.AddMinutes(30),
            SigningCredentials = credentials
        };
        await _logSvc.LogInfo("Populating token dict", "AUTHTokenGen");
        tokens.Add("accessToken", handler.WriteToken(handler.CreateToken(tokenDescriptor)));
        byte[] randStringToken = new byte[128];
        await _logSvc.LogInfo("Generating random refresh token", "AUTHTokenGen");
        RandomNumberGenerator.Create().GetBytes(randStringToken);
        tokens.Add("refreshToken", Convert.ToBase64String(randStringToken)!);

        await _logSvc.LogInfo($"Purging previous refresh tokens of user {user.Username}", "AUTHTokenGen");
        List<RefreshToken> tokensBelongingToUser = _db.RefreshTokens.Where(tk => tk.UserID == user.ID).ToList();
        foreach (RefreshToken i in tokensBelongingToUser)
        {
            _db.RefreshTokens.Remove(i);
        }
        await _logSvc.LogInfo("Saving newly generated token to DB", "AUTHTokenGen");
        _db.RefreshTokens.Add(new RefreshToken()
        {
            UserID = (long)user.ID!,
            user = user,
            Token = tokens["refreshToken"],
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow + TimeSpan.FromMinutes(10),
            isRevoked = false
        });

        await _db.SaveChangesAsync();
        await _logSvc.LogInfo("Tokenpair succesfully generated, passing result to the caller", "AUTHTokenGen");
        return tokens;
    }


    public async Task<ClaimsPrincipal> VerifyTokenAsync(string token)
    {
        await _logSvc.LogInfo("Verifying the token that's been provided by the caller", "AUTHTokenVerify");
        var handler = new JwtSecurityTokenHandler();
        await _logSvc.LogInfo("Instantiating object with validation parameters", "AUTHTokenVerify");
        ClaimsPrincipal claims;
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
            }, out _);
        }
        catch (Exception)
        {
            await _logSvc.LogError("Cound't verify a token", "AUTHTokenVerify");
            return null;
        }
        await _logSvc.LogInfo("Token successfully verified, passing claims to the caller", "AUTHTokenVerify");
        return claims;
    }


    public async Task<User?> UserByJWTAsync(HttpContext context)
    {
        await _logSvc.LogInfo("Initiated retrieving user by access token", "AUTHJWTToUSR");
        await _logSvc.LogInfo("Disassembling HTTP header", "AUTHJWTToUSR");
        string[] token = context.Request.Headers["Authorization"].ToString().Split(' ');
        if (token.Count() < 2) {
            await _logSvc.LogError("Incorrect header", "AUTHJWTToUSR");
            return null; }
        if (token[0] != "Bearer") {
            await _logSvc.LogError("Incorrect header", "AUTHJWTToUSR");
            return null; }
        ClaimsPrincipal decodedToken;
        await _logSvc.LogInfo("Invoking token verification", "AUTHJWTToUSR");
        decodedToken = await VerifyTokenAsync(token[1]);
        if (decodedToken == null)
        {
            await _logSvc.LogError("Invalid token", "AUTHJWTToUSR");
            return null;
        }
        await _logSvc.LogInfo("Extracting claims from token", "AUTHJWTToUSR");
        string username = decodedToken.Claims.Select(c => c.Value).ToList()[0];
        await _logSvc.LogInfo("Invoking GetUserAsync, passing in username", "AUTHJWTToUSR");
        return await _userSvc.GetUserAsync(new AuthDTO() { Username = username }, false);
    }
}