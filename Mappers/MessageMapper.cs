using MessagingApp.Models;
using MessagingApp.DTO;

namespace MessagingApp.Mappers;


public class MessageMapper
{
    public static MessageReceivedDTO MessageToReceivedMessage(Message message)
    {
        return new MessageReceivedDTO()
        {
            SenderUsername = message.OriginUser?.Username??"Unknown",
            Timestamp = message.Timestamp,
            MessageText = message.Text?? String.Empty,
            isRead = message.isRead

        };
    }
}