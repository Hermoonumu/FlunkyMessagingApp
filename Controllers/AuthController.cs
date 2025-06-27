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
        User newRegUser = await _userSvc.AddUser(uDTO);
        switch (newRegUser)
        {
            case User user: return Ok(newRegUser);
            case null: return StatusCode(400, "A user with such username already exists.");
        }
    } 
    #endregion
}