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
    public async Task<IActionResult> NewChatAsync([FromBody] ChatDescDTO ncDTO)
    {
        User user = await _authSvc.UserByJWTAsync(HttpContext);
        try
        {
            await _chtSvc.CreateNewChatAsync(user, ncDTO);
        }
        catch (ChatAlreadyExistsException e)
        {
            return BadRequest(e.Message);
        }

        return Ok($"Created chat named {ncDTO.Name}, owned by {user.Username}");
    }

    [Authorize]
    [HttpPost("newMember")]
    public async Task<IActionResult> AddMemberToChatAsync([FromBody] NewMemberChatDTO nmcDTO)
    {
        int countOfAdded = default;
        User user = await _authSvc.UserByJWTAsync(HttpContext);
        try
        {
            countOfAdded = await _chtSvc.AddMembersToChatAsync(user, nmcDTO);
        }
        catch (ChatDoesntExistException)
        {
            return NotFound("This chat doesn't exist");
        }
        catch (MemberAlreadyInChatException e)
        {
            return Ok(e.Message);
        }
        return Ok($"Added {countOfAdded} members");
    }

    [Authorize]
    [HttpGet("memberList")]
    public async Task<ActionResult<List<string>>> getChatMemebersAsync([FromQuery] string chatID)
    {
        User user = await _authSvc.UserByJWTAsync(HttpContext);

        try
        {
            return await _chtSvc.GetChatMembersAsync(user, long.Parse(chatID));
        }
        catch (FormatException)
        {
            return BadRequest("Invalid chatID");
        }
        catch (ChatDoesntExistException e)
        {
            return NotFound(e.Message);
        }
        catch (NotChatMemberException e)
        {
            return Unauthorized(e.Message);
        }

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
                                                                                        [FromQuery] string last = "10",
                                                                                        [FromQuery] string skip = "0",
                                                                                        [FromQuery] string unreadNotRead = "null",
                                                                                        [FromQuery] string polling = "false")
    {
        int _last;
        int _skip;
        long _chatid;
        bool? _unreadNotRead;
        bool _polling;
        try { _unreadNotRead = Boolean.Parse(unreadNotRead); }
        catch (FormatException) { _unreadNotRead = null; }
        try { long.TryParse(chatid, out _chatid); }
        catch (FormatException) { return BadRequest("Invalid chatid"); }
        try { int.TryParse(last, out _last); }
        catch (FormatException) { return BadRequest("Invalid last"); }
        try { int.TryParse(skip, out _skip); }
        catch (FormatException) { return BadRequest("Invalid last"); }
        try { _polling = Boolean.Parse(polling); }
        catch (FormatException) { _polling = false; }
        User user = await _authSvc.UserByJWTAsync(HttpContext);
        try
        {
            return Ok(await _chtSvc.ReadChatMsgs(_chatid, (long)user.ID!, _unreadNotRead, _skip, _last, _polling));
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

    [Authorize]
    [HttpDelete()]
    public async Task<IActionResult> DelChatAsync([FromBody] DelChatDTO dcDTO)
    {
        if (dcDTO.ID == null || dcDTO.ID == 0)
        {
            return BadRequest("Please specify chat ID");
        }
        User user = await _authSvc.UserByJWTAsync(HttpContext);
        try
        {
            await _chtSvc.DelChatAsync(user, (long)dcDTO.ID!);
        }
        catch (ChatDoesntExistException _)
        {
            return NotFound("Such chat couldn't be found");
        }
        catch (NotChatAdminException _)
        {
            return Unauthorized("You are not this chat's admin to delete it");
        }

        return Ok("Chat deleted");

    }

}