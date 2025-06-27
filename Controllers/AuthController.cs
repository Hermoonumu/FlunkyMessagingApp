using Microsoft.AspNetCore.Mvc;
using MessagingApp.DTO;
using MessagingApp.Mappers;
using MessagingApp.Models;
using MessagingApp.Services;
using Microsoft.AspNetCore.Identity;
using System.Diagnostics.Metrics;

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

    [HttpGet]
    public async Task<ActionResult<String>> Hello()
    {
        return StatusCode(200, "HIII!!!!");
    }

    [HttpGet("ExampleToken")]
    public async Task<ActionResult<string>> GetExampleJWTToken([FromBody] UserDTO uDTO)
    {
        User user = UserMapper.DTOToUser(uDTO);
        return StatusCode(200, _authSvc.GenerateToken(user));
    }

    #region "registering user"

    [HttpPost("register")]
    public async Task<ActionResult<User>> Register([FromBody] UserDTO uDTO)
    {
        if (uDTO.Username == null || uDTO.Password == null || String.Equals(uDTO.Password, "") || String.Equals(uDTO.Username, ""))
        {
            return StatusCode(400, "Bad Request, couldn't create a user, hit up API DOC");
        }
        PasswordHasher<User> passwordHasher = new PasswordHasher<User>();
        User newRegUser = new User();
        newRegUser.Username = uDTO.Username;
        newRegUser.PasswordHash = passwordHasher.HashPassword(newRegUser, uDTO.Password);
        newRegUser.Role = Models.User.RoleENUM.USER;
        switch (await _userSvc.AddUser(newRegUser))
        {
            case 0: return Ok(newRegUser);
            case 1: return StatusCode(400, "A user with such username already exists.");
        }
        return StatusCode(500, "sth happened yes, but tf I know what precisely");
    } 
    #endregion
}