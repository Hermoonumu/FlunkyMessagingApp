using MessagingApp.DTO;
using MessagingApp.Models;

namespace MessagingApp.Services;


public interface IMessageService
{
    public Task SendMessageAsync(User OriginUser, MessageSendForm msgSend);
    public Task<List<MessageReceivedDTO>> GetUserReceivedMessagesAsync(User user, bool? unreadNotRead = null, int last = 10, int skip = 0);
}