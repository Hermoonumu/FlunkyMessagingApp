using MessagingApp.DTO;
using MessagingApp.Exceptions;
using MessagingApp.Models;
using MessagingApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit.Sdk;

namespace MessagingApp.Controllers;

[ApiController]
[Route("/api/message")]
public class MessageController( IMessageService _msgSvc,
                                IAuthService _authSvc) : ControllerBase
{
    [Authorize]
    [HttpPost("send")]
    public async Task<ActionResult<string>> SendMessageAsync([FromBody] MessageSendForm msgSend)
    {
        try
        {
            await _msgSvc.SendMessageAsync(await _authSvc.UserByJWTAsync(HttpContext), msgSend);
        } catch (InvalidFormException) {
            return BadRequest("Check your message");
        } catch (UserDoesntExistException) {
            return NotFound("No such user");
        }
        return Ok("Message sent successfully");
    }
    [Authorize]
    [HttpGet("readReceived")]
    public async Task<ActionResult<List<MessageReceivedDTO>>> ReadMessagesAsync([FromQuery] string unreadNotRead="null",
                                                                                [FromQuery] string last = "10",
                                                                                [FromQuery] string skip = "0")
    {
        User user = await _authSvc.UserByJWTAsync(HttpContext);
        bool? _unreadNotRead;
        try { _unreadNotRead = Boolean.Parse(unreadNotRead); }
        catch (FormatException){ _unreadNotRead = null; }
        int _last;
        try { int.TryParse(last, out _last); }
        catch (FormatException) { return BadRequest("Invalid last"); }
        int _skip;
        try { int.TryParse(skip, out _skip); }
        catch (FormatException) { return BadRequest("Invalid skip"); }
        return Ok(await _msgSvc.GetUserReceivedMessagesAsync(user, _unreadNotRead, _last, _skip));
    }
}