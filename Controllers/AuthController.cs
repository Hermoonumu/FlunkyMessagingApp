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
                            ILoggerService _logSvc,
                            IValidationService _valid) : ControllerBase
{

    [HttpPost("ExampleToken")]
    public async Task<ActionResult<string>> GetExampleJWTToken([FromBody] AuthDTO urDTO)
    {
        await _logSvc.LogInfo($"{Request.Host} is trying to generate tokens for {urDTO.Username} user", "AUTH");
        User user = UserMapper.RegDTOToUser(urDTO);
        await _logSvc.LogInfo($"Mapped user AuthDTO to User", "AUTH");
        return StatusCode(200, _authSvc.GenerateTokensAsync(user));
    }

    #region "registering user"

    [HttpPost("register")]
    public async Task<ActionResult<User>> Register([FromBody] AuthDTO aDTO)
    {
        await _logSvc.LogInfo($"{Request.Host} is trying to register a new user", "AUTH");
        try { await _valid.AddUserValidateAsync(aDTO); }
        catch (InvalidFormException)
        {
            await _logSvc.LogError($"The reg form provided by {Request.Host} isn't valid", "AUTH");
            return StatusCode(400, "Invalid registration form.");
        }
        catch (UsernameTooShortException)
        {
            await _logSvc.LogError($"The username length provided by {Request.Host} is < 6 char", "AUTH");
            return StatusCode(400, "Username is less than 6 characters long.");
        }
        catch (PasswordTooShortException)
        {
            await _logSvc.LogError($"The password length provided by {Request.Host} is < 8 chars", "AUTH");
            return StatusCode(400, "Password is less than 8 characters long.");
        }
        await _logSvc.LogInfo($"Registering a user {aDTO.Username}", "AUTH");
        User newRegUser = await _userSvc.AddUserAsync(aDTO);
        switch (newRegUser)
        {
            case User user: return Ok(aDTO);
            case null:
                await _logSvc.LogError($"{Request.Host} tried to register under a uname that already exists", "AUTH");
                return StatusCode(400, "A user with such username already exists.");
        }
    }
    #endregion

    #region "loging user in"

    [HttpPost("login")]
    public async Task<ActionResult<string>> Login([FromBody] AuthDTO aDTO)
    {
        await _logSvc.LogInfo($"{Request.Host} is trying to obtain token-pair", "AUTH");
        try { await _userSvc.AuthenticateUserAsync(aDTO); }
        catch (UserDoesntExistException)
        {
            await _logSvc.LogError($"{aDTO.Username} doesn't exist", "AUTH");
            return StatusCode(404, "No such user");
        }
        catch (PasswordCheckFailedException)
        {
            await _logSvc.LogError($"Password supplied with {aDTO.Username} isn't correct", "AUTH");
            return StatusCode(401, "Incorrect password");
        }
        await _logSvc.LogInfo($"Proceeding to generation of a tokenpair for {aDTO.Username}", "AUTH");
        return Ok(await _authSvc.GenerateTokensAsync((await _userSvc.GetUserAsync(aDTO, false))!));
    }
    #endregion



    [Authorize]
    [HttpGet]
    public async Task<ActionResult<AuthDTO>> AuthTest()
    {
        await _logSvc.LogInfo($"{Request.Host} initiates the test of accessToken", "AUTH");
        await _logSvc.LogInfo($"Proceeding to decoding and retreiving a user from accessToken", "AUTH");
        User user = await _authSvc.UserByJWTAsync(HttpContext);
        if (user is null)
        {
            await _logSvc.LogError($"{Request.Host} has invalid token", "AUTH");
            return StatusCode(401, "nope, check ur token, or refresh it");
        }
        await _logSvc.LogInfo($"Sending {Request.Host} OK", "AUTH");
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