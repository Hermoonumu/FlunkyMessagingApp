using Microsoft.AspNetCore.Mvc;
using MessagingApp.DTO;
using MessagingApp.Models;
using MessagingApp.Services;
using Microsoft.AspNetCore.Authorization;
using MessagingApp.Exceptions;

namespace MessagingApp.Controllers;

//Контролер що опрацьовує авторизацію та автентифікацію
[ApiController]
[Route("/api/auth")]
public class AuthController(IUserService _userSvc,
                            IAuthService _authSvc,
                            IValidationService _valid,
                            ILogger<AuthController> _logger) : ControllerBase
{

    [HttpPost("RetrieveExpiredToken")]
    public async Task<ActionResult<string>> GetExampleJWTToken([FromBody] AuthDTO urDTO)
    {
        using (_logger.BeginScope("Username:{username}", urDTO.Username))
        {
            _logger.LogInformation($"Initiated test exprited JWT retrieval by {HttpContext.Request.Host}");
            User user = (await _userSvc.GetUserAsync(urDTO, false))!;
            _logger.LogInformation($"Returning JWT to {HttpContext.Request.Host}");
            return StatusCode(200, await _authSvc.GenerateTokensAsync(user, true));
        }

    }

    #region "registering user"

    [HttpPost("register")]
    public async Task<ActionResult<User>> Register([FromBody] AuthDTO aDTO)
    {
        using (_logger.BeginScope("Username:{username}", aDTO.Username))
        {
            _logger.LogInformation($"Scope initiated by {HttpContext.Request.Host}");
            _logger.LogInformation($"Initiated user registration by ");
            try { await _valid.AddUserValidateAsync(aDTO); }
            catch (InvalidFormException)
            {
                _logger.LogWarning($"Invalid registration form provided");
                return StatusCode(400, "Invalid registration form.");
            }
            catch (UsernameTooShortException)
            {
                _logger.LogWarning($"Invalid registration form (short username) provided");
                return StatusCode(400, "Username is less than 6 characters long.");
            }
            catch (PasswordTooShortException)
            {
                _logger.LogWarning($"Invalid registration form (short password) provided");
                return StatusCode(400, "Password is less than 8 characters long.");
            }
            _logger.LogInformation("Adding user to DB");
            bool regSuccess = await _userSvc.AddUserAsync(aDTO);
            switch (regSuccess)
            {
                case true:
                    _logger.LogInformation("User successfully created, returning HTTP OK");
                    return Ok("User succesfully created");
                case false:
                    _logger.LogWarning("A user with such username already exists.");
                    return StatusCode(409, "A user with such username already exists.");
            }
        }
        
    }
    #endregion

    #region "loging user in"

    [HttpPost("login")]
    public async Task<ActionResult<string>> Login([FromBody] AuthDTO aDTO)
    {
        using (_logger.BeginScope("Username:{username}", aDTO.Username))
        {
            _logger.LogInformation($"Scope initiated by {HttpContext.Request.Host}");
            _logger.LogInformation("Initiated login");
            try { await _userSvc.AuthenticateUserAsync(aDTO); }
            catch (UserDoesntExistException)
            {
                _logger.LogWarning("No such user for auth");
                return StatusCode(404, "No such user");
            }
            catch (PasswordCheckFailedException)
            {
                _logger.LogWarning("Password hashes don't coincide");
                return StatusCode(401, "Incorrect password");
            }
            _logger.LogInformation("Successfully authorized");
            return Ok(await _authSvc.GenerateTokensAsync((await _userSvc.GetUserAsync(aDTO, false))!));
        }

    }
    #endregion



    [Authorize]
    [HttpGet]
    public async Task<ActionResult<AuthDTO>> AuthTest()
    {
        User user = await _authSvc.UserByJWTAsync(HttpContext);
        using (_logger.BeginScope("Username:{username}", user.Username))
        {
            _logger.LogInformation($"Scope initiated by {HttpContext.Request.Host}");
            _logger.LogInformation("Initiated test auth");
            if (user is null)
            {
                _logger.LogWarning("Invalid token");
                return StatusCode(401, "nope, check ur token, or refresh it");
            }
            _logger.LogInformation("Successfully authorized");
            return Ok();  
        }
    }

    [Authorize]
    [HttpGet("logout")]
    public async Task<ActionResult<string>> Logout()
    {
        _logger.LogInformation($"Initiated session invalidation by {HttpContext.Request.Host}");
        string tokenToRevoke = await _authSvc.ExtractTokenFromHeaderAsync(HttpContext);
        await _authSvc.RevokeTokensAsync(tokenToRevoke);
        return Ok("Success");
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<string>> RefreshTokens([FromBody] RefreshTokenDTO rtDTO)
    {
        _logger.LogInformation($"Initiated token refresh by {HttpContext.Request.Host}");
        Dictionary<string, string>? refreshedTokens = await _authSvc.RefreshTokensAsync(rtDTO.refreshToken);
        if (refreshedTokens == null) {
            _logger.LogWarning($"Invalid refresh token provided by {HttpContext.Request.Host}. Refresh failed. {rtDTO.refreshToken}");
            return Unauthorized("Invalid refresh token"); }
        return Ok(refreshedTokens);
    }
}