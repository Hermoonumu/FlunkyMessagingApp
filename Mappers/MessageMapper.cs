using MessagingApp.Migrations;
using MessagingApp.Models;

namespace MessagingApp.DTO;


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