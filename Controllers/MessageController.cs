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
                                IAuthService _authSvc,
                                ILogger<MessageController> _logger) : ControllerBase
{
    [Authorize]
    [HttpPost("send")]
    public async Task<ActionResult<string>> SendMessageAsync([FromBody] MessageSendForm msgSend)
    {
        User? user = await _authSvc.UserByJWTAsync(HttpContext);
        using (_logger.BeginScope("Username:{username}", user.Username))
        {
            try
            {
                _logger.LogInformation($"Attempting to send a message to user {msgSend.DestinationUsername}");
                await _msgSvc.SendMessageAsync(user, msgSend);
            }
            catch (InvalidFormException)
            {
                _logger.LogWarning("Invalid message");
                return BadRequest("Check your message");
            }
            catch (UserDoesntExistException)
            {
                _logger.LogWarning("No such user to send message to");
                return NotFound("No such user");
            }
            _logger.LogInformation("Message sent successfully");
            return Ok("Message sent successfully");
        }
    }
    [Authorize]
    [HttpGet("readReceived")]
    public async Task<ActionResult<List<MessageReceivedDTO>>> ReadMessagesAsync([FromQuery] string unreadNotRead="null",
                                                                                [FromQuery] string last = "10",
                                                                                [FromQuery] string skip = "0")
    {
        User user = await _authSvc.UserByJWTAsync(HttpContext);
        using (_logger.BeginScope("Username:{username}", user.Username))
        {
            bool? _unreadNotRead;
            try { _unreadNotRead = Boolean.Parse(unreadNotRead); }
            catch (FormatException){ _unreadNotRead = null; }
            int _last;
            try { int.TryParse(last, out _last); }
            catch (FormatException) {
                _logger.LogWarning("Invalid last");
                return BadRequest("Invalid last"); }
            int _skip;
            try { int.TryParse(skip, out _skip); }
            catch (FormatException) {
                _logger.LogWarning("Invalid skip");
                return BadRequest("Invalid skip"); }
            return Ok(await _msgSvc.GetUserReceivedMessagesAsync(user, _unreadNotRead, _last, _skip));
        }
    }
}