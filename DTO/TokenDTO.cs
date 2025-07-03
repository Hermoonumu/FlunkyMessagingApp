
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace MessagingApp.DTO;

public class TokenDTO
{
    public ClaimsPrincipal claims { set; get; }
    public SecurityToken SecToken { set; get; }
}