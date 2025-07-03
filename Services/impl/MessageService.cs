using MessagingApp.DTO;
using MessagingApp.Models;
using Microsoft.EntityFrameworkCore;
namespace MessagingApp.Services.Implementation;


public class MessageService(DataContext _db, IUserService _userSvc, ILoggerService _logSvc) : IMessageService
{
    public async Task<int> SendMessageAsync(User OriginUser, MessageSendForm msgSend)
    {
        await _logSvc.LogInfo($"{OriginUser.Username} sends message to {msgSend.DestinationUsername}", "MSGSvc");
        User? DestinationUser = await _db.Users.FirstOrDefaultAsync(usr => usr.Username == msgSend.DestinationUsername);
        if (DestinationUser is null) {
            await _logSvc.LogError($"Couldn't find user {msgSend.DestinationUsername}", "MSGSvc");
            return 1; }
        if (msgSend.MessageText == String.Empty || msgSend.MessageText is null) {
            await _logSvc.LogError("Message is empty", "MSGSvc");
            return 2; }
        await _logSvc.LogInfo("Composing an instance of Message obj", "MSGSvc");
        Message message = new Message()
        {
            Text = msgSend.MessageText,
            OriginID = (long)OriginUser.ID!,
            DestinationID = DestinationUser.ID,
            Timestamp = DateTime.UtcNow
        };
        await _logSvc.LogInfo("Saving message to DB", "MSGSvc");
        _db.Messages.Add(message);
        await _db.SaveChangesAsync();
        await _logSvc.LogInfo("Message successfully sent", "MSGSvc");
        return 0;
    }

    public async Task<List<MessageReceivedDTO>> GetUserReceivedMessagesAsync(User user, bool? unreadNotRead = null)
    {
        await _logSvc.LogInfo($"{user.Username}" +
        $"tries to read {(unreadNotRead == null ? " all " : ((bool)unreadNotRead ? " unread " : " read "))}"
        + "messages", "MSGSvc");

        List<Message>? messages =
        (await _userSvc.GetUserAsync(new AuthDTO() { Username = user.Username }, true)).ReceivedMessages;

        if (messages == null) {
            await _logSvc.LogInfo("Nothing to read", "MSGSvc");
            return null; }
        await _logSvc.LogInfo("Instantiating new list of filtered messages", "MSGSvc");
        List<MessageReceivedDTO> messagesToShow = new List<MessageReceivedDTO>();
        switch (unreadNotRead)
        {
            case true:
                await _logSvc.LogInfo("Filtering msgs", "MSGSvc");
                messagesToShow = messages.Where(x => x.isRead == false).ToList()
                .ConvertAll(MessageMapper.MessageToReceivedMessage);
                await _logSvc.LogInfo("Ticking unread messages as read", "MSGSvc");
                foreach (Message i in messages) if (i.isRead == false) i.isRead = true;
                break;
            case false:
                await _logSvc.LogInfo("Filtering msgs", "MSGSvc");
                messagesToShow = messages.Where(msg => msg.isRead == true).ToList()
                .ConvertAll(MessageMapper.MessageToReceivedMessage);
                break;
            case null:
                await _logSvc.LogInfo("No filter is applied", "MSGSvc");
                messagesToShow = messages.ConvertAll(MessageMapper.MessageToReceivedMessage);
                break;
        }
        await _logSvc.LogInfo("Saving isRead flag of each msg to DB", "MSGSvc");
        _db.SaveChanges();
        await _logSvc.LogInfo("Successfully retrieved and filtered msgs, returning list of them to caller", "MSGSvc");
        return messagesToShow;
    }
}