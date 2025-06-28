using MessagingApp.DTO;
using MessagingApp.Models;
using MessagingApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MessagingApp.Controllers;

[ApiController]
[Route("/api/message")]
public class MessageController( IMessageService _msgSvc,
                                IAuthService _authSvc,
                                ILoggerService _logSvc) : ControllerBase
{
    [Authorize]
    [HttpPost("send")]
    public async Task<ActionResult<string>> SendMessageAsync([FromBody] MessageSendForm msgSend)
    {
        await _logSvc.LogInfo($"{Request.Host} attempts to send a message to {msgSend.DestinationUsername}", "MSG");
        await _logSvc.LogInfo("Invoking SendMessageAsync", "MSG");
        switch (await _msgSvc.SendMessageAsync(await _authSvc.UserByJWTAsync(HttpContext), msgSend))
        {
            case 0:
                await _logSvc.LogInfo("Message sent successfully", "MSG");
                return Ok("Message sent successfully");
            case 1:
                return NotFound("No such user");
            case 2: return BadRequest("Check your message");
            case 3: return Unauthorized("check ur token");
        }
        return StatusCode(100, "wut");
    }
    [Authorize]
    [HttpGet("readReceived")]
    public async Task<ActionResult<List<MessageReceivedDTO>>> ReadMessagesAsync([FromQuery] string unreadNotRead="null")
    {   Boolean.TryParse(unreadNotRead, out bool result);
        await _logSvc.LogInfo($"{Request.Host} attemts to read own "+
        $"{(unreadNotRead=="null"?" all ":(result?" unread ":" read "))} messages", "MSG");
        await _logSvc.LogInfo("Invoking UserByJWTAsync, passing HttpContext", "MSG");
        User user = await _authSvc.UserByJWTAsync(HttpContext);
        if (user is null) {
            await _logSvc.LogError("Token invalid, couldn't retrieve user", "MSG");
            return Unauthorized("check ur token"); }
        bool? state;
        try { state = Boolean.Parse(unreadNotRead); }
        catch (FormatException){ state = null; }
        await _logSvc.LogInfo("Invoking GetUserReceivedMessagesAsync, passing user and filter", "MSG");
        return Ok(await _msgSvc.GetUserReceivedMessagesAsync(user, state));
    }
}