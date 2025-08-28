using MessagingApp.DTO;
using MessagingApp.Models;

namespace MessagingApp.Services;


public interface IChatService
{
    public Task CreateNewChatAsync(User owner, NewChatDTO ncDTO);
    public Task SendMsgToChatAsync(long userid, SendChatMsgDTO scmDTO);
    public Task<List<MessageReceivedDTO>> ReadChatMsgs(long ChatID, long UserID, bool? unreadNotRead = null, int skip=0, int messagesSinceLast = 10, bool polling = false);
    public Task<Dictionary<string, long>> GetUserAvailableChats(long UserID);
    public Task DelChatAsync(User supposedOwner, long chatID);
    public Task<int> AddMembersToChatAsync(User owner, NewMemberChatDTO nmcDTO);
}