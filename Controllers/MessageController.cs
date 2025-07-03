using MessagingApp.DTO;
using MessagingApp.Exceptions;
using MessagingApp.Models;
using MessagingApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<ActionResult<List<MessageReceivedDTO>>> ReadMessagesAsync([FromQuery] string unreadNotRead="null")
    {   Boolean.TryParse(unreadNotRead, out bool result);
        User user = await _authSvc.UserByJWTAsync(HttpContext);
        if (user is null) {
            return Unauthorized("check ur token"); }
        bool? state;
        try { state = Boolean.Parse(unreadNotRead); }
        catch (FormatException){ state = null; }
        return Ok(await _msgSvc.GetUserReceivedMessagesAsync(user, state));
    }
}