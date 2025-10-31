using MessagingApp.DTO;
using MessagingApp.Exceptions;
using MessagingApp.Models;
using MessagingApp.Mappers;
using Microsoft.EntityFrameworkCore;
namespace MessagingApp.Services.Implementation;


public class MessageService(DataContext _db, ILogger<MessageService> _logger) : IMessageService
{
    public async Task SendMessageAsync(User OriginUser, MessageSendForm msgSend)
    {
        _logger.LogInformation("Sending message");
        User? DestinationUser = await _db.Users.FirstOrDefaultAsync(usr => usr.Username == msgSend.DestinationUsername);
        if (DestinationUser is null) {
            _logger.LogWarning("Couldn't find the user to send message to");
            throw new UserDoesntExistException();
        }
        if (msgSend.MessageText == String.Empty || msgSend.MessageText is null) {
            _logger.LogWarning("Invalid message");
            throw new InvalidFormException("Message is empty.");
        }
        _logger.LogInformation("Composing message");
        Message message = new Message()
        {
            Text = msgSend.MessageText,
            OriginID = (long)OriginUser.ID!,
            DestinationID = DestinationUser.ID,
            Timestamp = DateTime.UtcNow
        };
        _logger.LogInformation("Sending message and persisting changes");
        _db.Messages.Add(message);
        await _db.SaveChangesAsync();
    }

    public async Task<List<MessageReceivedDTO>> GetUserReceivedMessagesAsync(User user, bool? unreadNotRead = null, int last = 10, int skip=0)
    {
        _logger.LogInformation($"Reading user's messages (last:{last}, skip:{skip}, unreadNotRead:{unreadNotRead})");
        List<Message>? messages = await _db.Messages
                                    .Where(m => m.DestinationID == user.ID)
                                    .Include(m => m.OriginUser)
                                    .OrderByDescending(m => m.Timestamp)
                                    .Skip(skip)
                                    .Take(last) 
                                    .ToListAsync();

        if (messages == null) return null; 
        List<MessageReceivedDTO> messagesToShow = new List<MessageReceivedDTO>();
        _logger.LogInformation("Marking selected messages as read");
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
        _logger.LogInformation("Sending the message list");
        await _db.SaveChangesAsync();
        return messagesToShow;
    }
}