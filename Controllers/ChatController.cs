using MessagingApp.DTO;
using MessagingApp.Exceptions;
using MessagingApp.Models;
using MessagingApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MessagingApp.Controllers;


[ApiController]
[Route("/api/chats")]
public class ChatController(IAuthService _authSvc,
                            IChatService _chtSvc,
                            ILogger<ChatController> _logger) : ControllerBase
{

    [Authorize]
    [HttpPost("newChat")]
    public async Task<IActionResult> NewChatAsync([FromBody] ChatDescDTO ncDTO)
    {
        User user = await _authSvc.UserByJWTAsync(HttpContext);
        using (_logger.BeginScope("Username:{username}", user.Username))
        {
            _logger.LogInformation($"Scope initiated by {HttpContext.Request.Host}");
            try
            {
                _logger.LogInformation($"Attempting to create a chat named \"{ncDTO.Name}\"");
                await _chtSvc.CreateNewChatAsync(user, ncDTO);
            }
            catch (ChatAlreadyExistsException e)
            {
                _logger.LogWarning($"Couldn't create chat \"{ncDTO.Name}\", such exists already");
                return BadRequest(e.Message);
            }
            _logger.LogInformation($"Successfully created chat \"{ncDTO.Name}\"");
            return Ok($"Created chat named {ncDTO.Name}, owned by {user.Username}");  
        }
    }

    [Authorize]
    [HttpPost("newMember")]
    public async Task<IActionResult> AddMemberToChatAsync([FromBody] NewMemberChatDTO nmcDTO)
    {
        int countOfAdded = default;
        User user = await _authSvc.UserByJWTAsync(HttpContext);
        using (_logger.BeginScope("Username:{username}", user.Username))
        {
            _logger.LogInformation($"Scope initiated by {HttpContext.Request.Host}");
            try
            {
                _logger.LogInformation($"Attempting to add all the members into chat \"{nmcDTO.ChatID}\"");
                countOfAdded = await _chtSvc.AddMembersToChatAsync(user, nmcDTO);
            }
            catch (ChatDoesntExistException)
            {
                _logger.LogWarning("No such chat");
                return NotFound("This chat doesn't exist");
            }
            catch (MemberAlreadyInChatException e)
            {
                _logger.LogWarning(e.Message);
                return Ok(e.Message);
            }
            _logger.LogInformation("Successfully added all members");
            return Ok($"Added {countOfAdded} members");
        }
    }

    [Authorize]
    [HttpGet("memberList")]
    public async Task<ActionResult<List<string>>> getChatMemebersAsync([FromQuery] string chatID)
    {
        User user = await _authSvc.UserByJWTAsync(HttpContext);
        using (_logger.BeginScope("Username:{username}", user.Username))
        {
            _logger.LogInformation($"Scope initiated by {HttpContext.Request.Host}");
            try
            {
                _logger.LogInformation("Attempting to get chat members");
                return await _chtSvc.GetChatMembersAsync(user, long.Parse(chatID));
            }
            catch (FormatException)
            {
                _logger.LogWarning("Invalid chatID by client");
                return BadRequest("Invalid chatID");
            }
            catch (ChatDoesntExistException e)
            {
                _logger.LogWarning("No such chat exists");
                return NotFound(e.Message);
            }
            catch (NotChatMemberException e)
            {
                _logger.LogWarning("The client isn't a chat member to obtain access to this resource");
                return Unauthorized(e.Message);
            }
        }

    }


    [Authorize]
    [HttpGet("myChats")]
    public async Task<ActionResult<Dictionary<string, long>>> GetUserChatsAsync()
    {
        _logger.LogInformation($"Initiated chats retrieval by {HttpContext.Request.Host}");
        User user = await _authSvc.UserByJWTAsync(HttpContext);
        return Ok(await _chtSvc.GetUserAvailableChats((long)user.ID!));
    }

    [Authorize]
    [HttpPost("sendMsgToChat")]
    public async Task<IActionResult> SendMsgToChatAsync([FromBody] SendChatMsgDTO scmForm)
    {
        User user = await _authSvc.UserByJWTAsync(HttpContext);
        using (_logger.BeginScope("Username:{username}", user.Username))
        {
            _logger.LogInformation($"Scope initiated by {HttpContext.Request.Host}");
            try
            {
                _logger.LogInformation("Attempting to send a message to chat");
                await _chtSvc.SendMsgToChatAsync((long)user.ID!, scmForm);
            }
            catch (ChatDoesntExistException)
            {
                _logger.LogWarning("No such chat");
                return NotFound("Such chat doesn't exist");
            }
            catch (NotChatMemberException)
            {
                _logger.LogWarning("You are not a member of this chat");
                return Unauthorized("You are not a member of this chat");
            }
            _logger.LogInformation("Message sent");
            return Ok("Message sent");
        }

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
        catch (FormatException) {
            _logger.LogWarning($"Invalid unreadNotRead {HttpContext.Request.Host}");
            _unreadNotRead = null; }
        try { long.TryParse(chatid, out _chatid); }
        catch (FormatException) {
            _logger.LogWarning($"Invalid chatid {HttpContext.Request.Host}");
            return BadRequest("Invalid chatid"); }
        try { int.TryParse(last, out _last); }
        catch (FormatException) {
            _logger.LogWarning($"Invalid last {HttpContext.Request.Host}");
            return BadRequest("Invalid last"); }
        try { int.TryParse(skip, out _skip); }
        catch (FormatException) {
            _logger.LogWarning($"Invalid skip {HttpContext.Request.Host}");
            return BadRequest("Invalid last"); }
        try { _polling = Boolean.Parse(polling); }
        catch (FormatException) { _polling = false; }
        User user = await _authSvc.UserByJWTAsync(HttpContext);
        using (_logger.BeginScope("Username:{username}", user.Username)) {
            try
            {
                _logger.LogInformation("Obtaining messages from chat...");
                return Ok(await _chtSvc.ReadChatMsgs(_chatid, (long)user.ID!, _unreadNotRead, _skip, _last, _polling));
            }
            catch (NotChatMemberException)
            {
                _logger.LogWarning("Not a chat member");
                return Unauthorized("Not a chat member");
            }
            catch (ChatDoesntExistException)
            {
                _logger.LogWarning("No such chat exists");
                return BadRequest("No such chat exists");
            }
        }
    }

    [Authorize]
    [HttpDelete()]
    public async Task<IActionResult> DelChatAsync([FromBody] DelChatDTO dcDTO)
    {
        _logger.LogInformation("Initiating chat deletion");
        if (dcDTO.ID == null || dcDTO.ID == 0)
        {
            _logger.LogWarning("No chatID, has to be specified");
            return BadRequest("Please specify chat ID");
        }
        User user = await _authSvc.UserByJWTAsync(HttpContext);
        using (_logger.BeginScope("Username:{username}", user.Username))
        {
            try
            {
                _logger.LogInformation("Attempting to delete chat...");
                await _chtSvc.DelChatAsync(user, (long)dcDTO.ID!);
            }
            catch (ChatDoesntExistException _)
            {
                _logger.LogWarning("Such chat couldn't be found");
                return NotFound("Such chat couldn't be found");
            }
            catch (NotChatAdminException _)
            {
                _logger.LogWarning("This user is not this chat's admin to delete it");
                return Unauthorized("You are not this chat's admin to delete it");
            }
            _logger.LogInformation("Chat deleted");
            return Ok("Chat deleted");
        }

    }

}