using MessagingApp.DTO;
using MessagingApp.Exceptions;
using MessagingApp.Models;
using MessagingApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MessagingApp.Controllers;


[ApiController]
[Route("/api/chats")]
public class ChatController(IAuthService _authSvc, IMessageService _msgSvc, IChatService _chtSvc) : ControllerBase
{

    [Authorize]
    [HttpPost("newChat")]
    public async Task<IActionResult> NewChatAsync([FromBody] NewChatDTO ncDTO)
    {
        User user = await _authSvc.UserByJWTAsync(HttpContext);
        await _chtSvc.CreateNewChatAsync(user, ncDTO);
        return Ok($"Created chat named {ncDTO.Name}, owned by {user.Username}");
    }

    [Authorize]
    [HttpGet("myChats")]
    public async Task<ActionResult<Dictionary<string, long>>> GetUserChatsAsync()
    {
        User user = await _authSvc.UserByJWTAsync(HttpContext);
        return Ok(await _chtSvc.GetUserAvailableChats((long)user.ID!));
    }

    [Authorize]
    [HttpPost("sendMsgToChat")]
    public async Task<IActionResult> SendMsgToChatAsync([FromBody] SendChatMsgDTO scmForm)
    {
        User user = await _authSvc.UserByJWTAsync(HttpContext);
        try
        {
            await _chtSvc.SendMsgToChatAsync((long)user.ID!, scmForm);
        }
        catch (ChatDoesntExistException)
        {
            return BadRequest("Such chat doesn't exist");
        }
        catch (NotChatMemberException)
        {
            return Unauthorized("You are not a member of this chat");
        }
        return Ok("Message sent");
    }

    [Authorize]
    [HttpGet("readChatMsgs")]
    public async Task<ActionResult<List<MessageReceivedDTO>>> ReadMessagesFromChatAsync([FromQuery] string chatid,
                                                                                        [FromQuery] string last="10")
    {
        int _last = 0;
        long _chatid = 0;
        if (String.Empty != chatid) {
            if (!long.TryParse(chatid, out _chatid))
            {
                return BadRequest("Invalid chatid");
            } }
        if (String.Empty != last) {
            if (!int.TryParse(last, out _last))
            {
                return BadRequest("Invalid last"); 
            }
        }
        User user = await _authSvc.UserByJWTAsync(HttpContext);
        try
        {
            return Ok(await _chtSvc.ReadChatMsgs(_chatid, (long)user.ID!, _last));
        }
        catch (NotChatMemberException)
        {
            return Unauthorized("Not a chat member");
        }
        catch (ChatDoesntExistException)
        {
            return BadRequest("No such chat exists");
        }
    }
}