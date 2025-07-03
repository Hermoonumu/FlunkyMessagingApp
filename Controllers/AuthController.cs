using Microsoft.AspNetCore.Mvc;
using MessagingApp.DTO;
using MessagingApp.Mappers;
using MessagingApp.Models;
using MessagingApp.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography.X509Certificates;
using MessagingApp.Exceptions;

namespace MessagingApp.Controllers;


[ApiController]
[Route("/api/auth")]
public class AuthController(IUserService _userSvc,
                            IAuthService _authSvc,
                            IValidationService _valid) : ControllerBase
{

    [HttpPost("ExampleToken")]
    public async Task<ActionResult<string>> GetExampleJWTToken([FromBody] AuthDTO urDTO)
    {
        User user = UserMapper.RegDTOToUser(urDTO);
        return StatusCode(200, _authSvc.GenerateTokensAsync(user));
    }

    #region "registering user"

    [HttpPost("register")]
    public async Task<ActionResult<User>> Register([FromBody] AuthDTO aDTO)
    {
        try { await _valid.AddUserValidateAsync(aDTO); }
        catch (InvalidFormException)
        {
            return StatusCode(400, "Invalid registration form.");
        }
        catch (UsernameTooShortException)
        {
            return StatusCode(400, "Username is less than 6 characters long.");
        }
        catch (PasswordTooShortException)
        {
            return StatusCode(400, "Password is less than 8 characters long.");
        }
        User newRegUser = await _userSvc.AddUserAsync(aDTO);
        switch (newRegUser)
        {
            case User user: return Ok(aDTO);
            case null:
                return StatusCode(400, "A user with such username already exists.");
        }
    }
    #endregion

    #region "loging user in"

    [HttpPost("login")]
    public async Task<ActionResult<string>> Login([FromBody] AuthDTO aDTO)
    {
        try { await _userSvc.AuthenticateUserAsync(aDTO); }
        catch (UserDoesntExistException)
        {
            return StatusCode(404, "No such user");
        }
        catch (PasswordCheckFailedException)
        {
            return StatusCode(401, "Incorrect password");
        }
        return Ok(await _authSvc.GenerateTokensAsync((await _userSvc.GetUserAsync(aDTO, false))!));
    }
    #endregion



    [Authorize]
    [HttpGet]
    public async Task<ActionResult<AuthDTO>> AuthTest()
    {
        User user = await _authSvc.UserByJWTAsync(HttpContext);
        if (user is null)
        {
            return StatusCode(401, "nope, check ur token, or refresh it");
        }
        return Ok(UserMapper.UserToAuthDTO(user));
    }

    [Authorize]
    [HttpGet("logout")]
    public async Task<ActionResult<string>> Logout()
    {
        string tokenToRevoke = await _authSvc.ExtractTokenFromHeaderAsync(HttpContext);
        await _authSvc.RevokeTokensAsync(tokenToRevoke);
        return Ok("Success");
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<string>> RefreshTokens([FromBody] RefreshTokenDTO rtDTO)
    {
        Dictionary<string, string>? refreshedTokens = await _authSvc.RefreshTokensAsync(rtDTO.refreshToken);
        if (refreshedTokens == null) { return Unauthorized("Invalid refresh token"); }
        return Ok(refreshedTokens);
    }
}