using MessagingApp.Models;
using MessagingApp.DTO;

namespace MessagingApp.Mappers;


public class MessageMapper
{
    public static MessageReceivedDTO MessageToReceivedMessage(Message message)
    {
        return new MessageReceivedDTO()
        {
            SenderUsername = message.OriginUser.Username,
            Timestamp = message.Timestamp,
            MessageText = message.Text,
            isRead = message.isRead
        };
    }
}