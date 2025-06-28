using MessagingApp.DTO;
using MessagingApp.Models;
using MessagingApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MessagingApp.Controllers;

[ApiController]
[Route("/api/message")]
public class MessageController(IMessageService _msgSvc, IAuthService _authSvc) : ControllerBase
{
    [Authorize]
    [HttpPost("send")]
    public async Task<ActionResult<string>> SendMessageAsync([FromBody] MessageSendForm msgSend)
    {

        switch (await _msgSvc.SendMessageAsync(await _authSvc.UserByJWTAsync(HttpContext), msgSend))
        {
            case 0: return Ok("Message sent successfully");
            case 1: return NotFound("No such user");
            case 2: return BadRequest("Check your message");
            case 3: return Unauthorized("check ur token");
        }
        return StatusCode(100, "wut");
    }
    [Authorize]
    [HttpGet("readReceived")]
    public async Task<ActionResult<List<MessageReceivedDTO>>> ReadMessagesAsync([FromQuery] string unreadNotRead="null")
    {
        User user = await _authSvc.UserByJWTAsync(HttpContext);
        if (user is null) return Unauthorized("check ur token");
        bool? state;
        try { state = Boolean.Parse(unreadNotRead); }
        catch (FormatException){ state = null; }
        
        return Ok(await _msgSvc.GetUserReceivedMessagesAsync(user, state));
    }
}