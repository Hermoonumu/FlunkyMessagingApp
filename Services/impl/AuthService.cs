using System.IdentityModel.Tokens.Jwt;
using System.Text;
using MessagingApp.Models;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using MessagingApp.DTO;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.WebUtilities;
namespace MessagingApp.Services.Implementation;


public class AuthService : IAuthService
{
    private readonly JwtSecurityTokenHandler _jwtHandler = new JwtSecurityTokenHandler();
    private readonly TokenValidationParameters _tokenValidationParameters;
    private readonly IConfiguration _conf;
    private readonly DataContext _db;
    private readonly IUserService _userSvc;
    private readonly ILogger<AuthService> _logger;

    public AuthService(DataContext db,
                        IUserService userSvc,
                        IConfiguration conf,
                        ILogger<AuthService> logger)
    {
        _db = db;
        _userSvc = userSvc;
        _conf = conf;
        _logger = logger;
        var sec = _conf.GetSection("SecConfig");
        var key = sec.GetValue<string>("PrivateKey") ?? throw new ArgumentNullException("PrivateKey");
        var issuer = sec.GetValue<string>("Issuer") ?? throw new ArgumentNullException("Issuer");
        var audience = sec.GetValue<string>("Audience") ?? throw new ArgumentNullException("Audience");
        _tokenValidationParameters = new TokenValidationParameters
        {
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30) // optional
        };
    }
    public ClaimsIdentity createClaims(User user)
    {
        _logger.LogInformation("Generating claims for " + user.Username);
        ClaimsIdentity claims = new ClaimsIdentity();
        claims.AddClaim(new Claim(ClaimTypes.Name, user.Username!));
        claims.AddClaim(new Claim(ClaimTypes.Role, user.Role.ToString()!));
        claims.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.ID.ToString()!));
        claims.AddClaim(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
        return claims;
    }

    public async Task<Dictionary<string, string>> GenerateTokensAsync(User user, bool expired = false)
    {
        _logger.LogInformation("Generating token pair");
        Dictionary<string, string> tokens = new Dictionary<string, string>();
        JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
        _logger.LogInformation("Obtaining private signing key");
        byte[] key = Encoding.ASCII.GetBytes(_conf.GetSection("SecConfig").GetValue<String>("PrivateKey")!);
        SigningCredentials credentials = new SigningCredentials
            (
                new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha512Signature
            );
        _logger.LogInformation("Instantiating token descriptor");
        SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = createClaims(user),
            Issuer = _conf.GetSection("SecConfig").GetValue<String>("Issuer"),
            Audience = _conf.GetSection("SecConfig").GetValue<String>("Audience"),
            Expires = DateTime.Now.Add(expired?TimeSpan.FromSeconds(0):TimeSpan.FromMinutes(15)),
            SigningCredentials = credentials
        };
        _logger.LogInformation("Creating access token");
        tokens.Add("accessToken", handler.WriteToken(handler.CreateToken(tokenDescriptor)));
        _logger.LogInformation("Creating refresh token");
        byte[] randStringToken = new byte[128];
        RandomNumberGenerator.Create().GetBytes(randStringToken);
        tokens.Add("refreshToken", Convert.ToBase64String(randStringToken)!);
        _logger.LogInformation("Obtaining all refresh tokens owned by the user");
        List<RefreshToken> tokensBelongingToUser = _db.RefreshTokens.Where(tk => tk.UserID == user.ID).ToList();
        foreach (RefreshToken i in tokensBelongingToUser)
        {
            _logger.LogInformation("Removed refresh token");
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

        _logger.LogInformation("Saving tokens, sending response.");

        await _db.SaveChangesAsync();
        return tokens;
    }


    public (ClaimsPrincipal? Principal, JwtSecurityToken? Jwt) ValidateToken(string token)
    {
        _logger.LogInformation("Validating token");
        try
        {
            var principal = _jwtHandler.ValidateToken(token, _tokenValidationParameters, out SecurityToken? validatedToken);
            if (validatedToken is JwtSecurityToken jwt)
            {
                _logger.LogInformation("Token is valid");
                return (principal, jwt);
            }
            _logger.LogWarning("Invalid token");
            return (null, null);
        }
        catch (SecurityTokenException)
        {
            _logger.LogWarning("Invalid token");
            return (null, null);
        }
        catch (Exception)
        {
            _logger.LogError("Something went wrong during token validation");
            return (null, null);
        }
    }


    public async Task<User?> UserByJWTAsync(HttpContext context)
    {
        _logger.LogInformation("Retrieving user by the token");
        ClaimsIdentity decodedToken;
        decodedToken = (context.User.Identity as ClaimsIdentity)!;
        string username = decodedToken.Claims.Select(c => c.Value).ToList()[0];
        return await _userSvc.GetUserAsync(new AuthDTO() { Username = username }, false);
    }

    public async Task<string> ExtractTokenFromHeaderAsync(HttpContext context)
    {
        _logger.LogInformation("Obtaining the token from HTTP header");
        string[] token = context.Request.Headers["Authorization"].ToString().Split(' ');
        return token[1];
    }


    public async Task RevokeTokensAsync(string token)
    {
        _logger.LogInformation("Revoking tokens");
        var (claimsPrincipal, jwt) = ValidateToken(token);

        if (claimsPrincipal == null || jwt == null) { _logger.LogInformation("Nothing to revoke"); return; };
        var jti = jwt.Id ?? string.Empty;
        Claim? idClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)
                         ?? claimsPrincipal.FindFirst("sub")
                         ?? claimsPrincipal.FindFirst("id");

        if (idClaim == null || !long.TryParse(idClaim.Value, out long userId))
        { _logger.LogWarning("Invalid token"); return;}

        _logger.LogInformation("Revoking JWT");
        await _db.revokedJWTs.AddAsync(new RevokedJWTs { Token = token, JTI = jti });
        var refreshToken = await _db.RefreshTokens.FirstOrDefaultAsync(tk => tk.UserID == userId);
        _logger.LogInformation("Revoking refresh token");
        if (refreshToken != null) _db.RefreshTokens.Remove(refreshToken);
        _logger.LogInformation("Saving changes");
        await _db.SaveChangesAsync();
    }




    public async Task<Dictionary<string, string>?> RefreshTokensAsync(string refreshToken)
    {
        _logger.LogInformation("Refreshing tokens");
        RefreshToken? token = await _db.RefreshTokens.FirstOrDefaultAsync(tk => tk.Token == refreshToken);
        if (token == null) { _logger.LogWarning("No such refresh token"); return null; }
        if (token.ExpiresAt.CompareTo(DateTime.Now)<=0){
            _logger.LogWarning("Expired refresh token, deleting from DB");
            _db.RefreshTokens.Remove(token);
            return null; }
        User user = (await _db.Users.FirstOrDefaultAsync(u => u.ID == token.UserID))!;
        return await GenerateTokensAsync(user);
    }
}