using Microsoft.AspNetCore.Mvc;
using MessagingApp.DTO;
using MessagingApp.Mappers;
using MessagingApp.Models;
using MessagingApp.Services;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MessagingApp.Controllers;


[ApiController]
[Route("/api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserService _userSvc;
    private readonly AuthService _authSvc;

    public AuthController(UserService userSvc, AuthService auth)
    {
        _authSvc = auth;
        _userSvc = userSvc;
    }


    [HttpPost("ExampleToken")]
    public async Task<ActionResult<string>> GetExampleJWTToken([FromBody] AuthDTO urDTO)
    {
        User user = UserMapper.RegDTOToUser(urDTO);
        return StatusCode(200, _authSvc.GenerateTokens(user));
    }

    #region "registering user"

    [HttpPost("register")]
    public async Task<ActionResult<User>> Register([FromBody] AuthDTO aDTO)
    {
        int validationResult = await aDTO.AddUserValidate();
        if (validationResult == 1) return StatusCode(400, "Invalid registration form.");
        if (validationResult == 2) return StatusCode(400, "Username is less than 6 characters long.");
        if (validationResult == 3) return StatusCode(400, "Password is less than 8 characters long.");
        User newRegUser = await _userSvc.AddUser(aDTO);
        switch (newRegUser)
        {
            case User user: return Ok(newRegUser);
            case null: return StatusCode(400, "A user with such username already exists.");
        }
    }
    #endregion

    #region "loging user in"

    [HttpPost("login")]
    public async Task<ActionResult<string>> Login([FromBody] AuthDTO aDTO)
    {
        int authResult = await _userSvc.AuthenticateUser(aDTO);
        if (authResult == 1) return StatusCode(404, "No such user");
        if (authResult == 2) return StatusCode(401, "Incorrect password");

        return Ok(await _authSvc.GenerateTokens(_userSvc.GetUser(aDTO).Result));
    }
    #endregion



    [Authorize]
    [HttpGet]
    public async Task<ActionResult<object>> AuthTest()
    {
        string token = HttpContext.Request.Headers["Authorization"].ToString().Split(' ')[1];
        User user = await _authSvc.UserByJWTClaims(token);
        if (user is null) return StatusCode(401, "nope, check ur token, or refresh it");
        return Ok(Mappers.UserMapper.UserToAuthDTO(user));
    }
}