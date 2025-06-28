using MessagingApp.DTO;
using MessagingApp.Models;
using Microsoft.EntityFrameworkCore;
namespace MessagingApp.Services.Implementation;


public class MessageService(DataContext _db, IUserService _userSvc) : IMessageService
{
    public async Task<int> SendMessageAsync(User OriginUser, MessageSendForm msgSend)
    {

        User? DestinationUser = await _db.Users.FirstOrDefaultAsync(usr => usr.Username == msgSend.DestinationUsername);
        if (DestinationUser is null) return 1;
        if (msgSend.MessageText == String.Empty || msgSend.MessageText is null) return 2;
        Message message = new Message()
        {
            Text = msgSend.MessageText,
            OriginID = (long)OriginUser.ID!,
            DestinationID = DestinationUser.ID,
            Timestamp = DateTime.UtcNow
        };
        _db.Messages.Add(message);
        await _db.SaveChangesAsync();
        return 0;
    }

    public async Task<List<MessageReceivedDTO>> GetUserReceivedMessagesAsync(User user, bool? unreadNotRead = null)
    {
        List<Message> messages = (await _db.Users
        .Include(r => r.ReceivedMessages)
        .ThenInclude(ou => ou.OriginUser)
        .FirstOrDefaultAsync(u => u.ID == user.ID))!
        .ReceivedMessages;
        
        if (messages == null) return null;
        List<MessageReceivedDTO> messagesToShow = new List<MessageReceivedDTO>();
        switch (unreadNotRead)
        {
            case true:
                messagesToShow = messages.Where(x => x.isRead == false).ToList()
                .ConvertAll(MessageMapper.MessageToReceivedMessage);
                foreach (Message i in messages) if (i.isRead == false) i.isRead = true;
                break;
            case false:
                messagesToShow = messages.Where(msg => msg.isRead == true).ToList()
                .ConvertAll(MessageMapper.MessageToReceivedMessage);
                break;
            case null:
                messagesToShow = messages.ConvertAll(MessageMapper.MessageToReceivedMessage);
                break;
        }

        _db.SaveChanges();
        return messagesToShow;
    }
}