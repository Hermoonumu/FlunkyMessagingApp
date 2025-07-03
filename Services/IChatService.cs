using MessagingApp.DTO;
using MessagingApp.Models;

namespace MessagingApp.Services;


public interface IChatService
{
    public Task CreateNewChatAsync(User owner, NewChatDTO ncDTO);
    public Task SendMsgToChatAsync(long userid, SendChatMsgDTO scmDTO);
    public Task<List<MessageReceivedDTO>> ReadChatMsgs(long ChatID, long UserID, int messagesSinceLast = 10);
    public Task<Dictionary<string, long>> GetUserAvailableChats(long UserID);
}