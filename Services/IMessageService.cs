using MessagingApp.DTO;
using MessagingApp.Models;

namespace MessagingApp.Services;


public interface IMessageService
{
    public Task<int> SendMessageAsync(User OriginUser, MessageSendForm msgSend);
    public Task<List<MessageReceivedDTO>> GetUserReceivedMessagesAsync(User user, bool? unreadNotRead = null);
}