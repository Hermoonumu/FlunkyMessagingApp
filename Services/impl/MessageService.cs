using MessagingApp.DTO;
using MessagingApp.Exceptions;
using MessagingApp.Models;
using Microsoft.EntityFrameworkCore;
namespace MessagingApp.Services.Implementation;


public class MessageService(DataContext _db, IUserService _userSvc) : IMessageService
{
    public async Task SendMessageAsync(User OriginUser, MessageSendForm msgSend)
    {
        
        User? DestinationUser = await _db.Users.FirstOrDefaultAsync(usr => usr.Username == msgSend.DestinationUsername);
        if (DestinationUser is null) {
            throw new UserDoesntExistException();}
        if (msgSend.MessageText == String.Empty || msgSend.MessageText is null) {
            throw new InvalidFormException("Message is empty.");}
        Message message = new Message()
        {
            Text = msgSend.MessageText,
            OriginID = (long)OriginUser.ID!,
            DestinationID = DestinationUser.ID,
            Timestamp = DateTime.UtcNow
        };
        _db.Messages.Add(message);
        await _db.SaveChangesAsync();
    }

    public async Task<List<MessageReceivedDTO>> GetUserReceivedMessagesAsync(User user, bool? unreadNotRead = null)
    {

        List<Message>? messages =
        (await _userSvc.GetUserAsync(new AuthDTO() { Username = user.Username }, true)).ReceivedMessages;

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
                foreach (Message i in messages) if (i.isRead == false) i.isRead = true;
                break;
        }
        _db.SaveChanges();
        return messagesToShow;
    }
}