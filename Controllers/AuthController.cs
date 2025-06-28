using Microsoft.AspNetCore.Mvc;
using MessagingApp.DTO;
using MessagingApp.Mappers;
using MessagingApp.Models;
using MessagingApp.Services;
using Microsoft.AspNetCore.Authorization;

namespace MessagingApp.Controllers;


[ApiController]
[Route("/api/auth")]
public class AuthController : ControllerBase
{

    private readonly ILoggerService _logSvc;
    private readonly IAuthService _authSvc;
    private readonly IUserService _userSvc;

    public AuthController(IUserService userSvc,
                            IAuthService authSvc,
                            ILoggerService logSvc)
    {
        _userSvc = userSvc;
        _authSvc = authSvc;
        _logSvc = logSvc;
    }


    [HttpPost("ExampleToken")]
    public async Task<ActionResult<string>> GetExampleJWTToken([FromBody] AuthDTO urDTO)
    {
        _logSvc.LogInfo($"{Request.Host} is trying to generate tokens for {urDTO.Username} user", "AUTH");
        User user = UserMapper.RegDTOToUser(urDTO);
        _logSvc.LogInfo($"Mapped user AuthDTO to User", "AUTH");
        return StatusCode(200, _authSvc.GenerateTokensAsync(user));
    }

    #region "registering user"

    [HttpPost("register")]
    public async Task<ActionResult<User>> Register([FromBody] AuthDTO aDTO)
    {
        _logSvc.LogInfo($"{Request.Host} is trying to register a new user", "AUTH");
        int validationResult = await aDTO.AddUserValidate();
        if (validationResult == 1)
        {
            _logSvc.LogError($"The reg form provided by {Request.Host} isn't valid", "AUTH");
            return StatusCode(400, "Invalid registration form.");
        }
        if (validationResult == 2) {
            _logSvc.LogError($"The username length provided by {Request.Host} is < 6 char", "AUTH");
            return StatusCode(400, "Username is less than 6 characters long."); }
        if (validationResult == 3) {
            _logSvc.LogError($"The password length provided by {Request.Host} is < 8 chars", "AUTH");
            return StatusCode(400, "Password is less than 8 characters long."); }
        _logSvc.LogInfo($"Registering a user {aDTO.Username}", "AUTH");
        User newRegUser = await _userSvc.AddUserAsync(aDTO);
        switch (newRegUser)
        {
            case User user: return Ok(aDTO);
            case null:
                _logSvc.LogError($"{Request.Host} tried to register under a uname that already exists", "AUTH");
                return StatusCode(400, "A user with such username already exists.");
        }
    }
    #endregion

    #region "loging user in"

    [HttpPost("login")]
    public async Task<ActionResult<string>> Login([FromBody] AuthDTO aDTO)
    {
        _logSvc.LogInfo($"{Request.Host} is trying to obtain token-pair", "AUTH");
        int authResult = await _userSvc.AuthenticateUserAsync(aDTO);
        if (authResult == 1) {
            _logSvc.LogError($"{aDTO.Username} doesn't exist", "AUTH");
            return StatusCode(404, "No such user"); }
        if (authResult == 2) {
            _logSvc.LogError($"Password supplied with {aDTO.Username} isn't correct", "AUTH");
            return StatusCode(401, "Incorrect password"); }
        _logSvc.LogInfo($"Proceeding to generation of a tokenpair for {aDTO.Username}", "AUTH");
        return Ok(await _authSvc.GenerateTokensAsync((await _userSvc.GetUserAsync(aDTO, false))!));
    }
    #endregion



    [Authorize]
    [HttpGet]
    public async Task<ActionResult<object>> AuthTest()
    {
        _logSvc.LogInfo($"{Request.Host} initiates the test of accessToken", "AUTH");
        _logSvc.LogInfo($"Proceeding to decoding and retreiving a user from accessToken", "AUTH");
        User user = await _authSvc.UserByJWTAsync(HttpContext);
        if (user is null) {
            _logSvc.LogError($"{Request.Host} has invalid token", "AUTH");
            return StatusCode(401, "nope, check ur token, or refresh it"); }
        _logSvc.LogInfo($"Sending {Request.Host} OK", "AUTH");
        return Ok(UserMapper.UserToAuthDTO(user));
    }
}