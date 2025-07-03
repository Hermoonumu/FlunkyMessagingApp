namespace MessagingApp.Services.Implementation;

using MessagingApp.DTO;
using MessagingApp.Exceptions;
using MessagingApp.Models;
using MessagingApp.Services;
using Microsoft.EntityFrameworkCore;
using MessagingApp.Mappers;
using System.Security.Cryptography.X509Certificates;

public class ChatService(DataContext _db) : IChatService
{

    public async Task CreateNewChatAsync(User owner, NewChatDTO ncDTO)
    {
        Chat chat = new Chat()
        {
            Owner = owner,
            Name = ncDTO.Name
        };
        chat.Members.Add(owner);
        foreach (string i in ncDTO.Members)
        {
            User? newMember = await _db.Users.FirstOrDefaultAsync(u => u.Username == i);
            if (newMember != null) chat.Members.Add(newMember);
        }
        await _db.Chats.AddAsync(chat);
        await _db.SaveChangesAsync();
    }

    public async Task<Dictionary<string, long>> GetUserAvailableChats(long UserID)
    {
        User user = (await _db.Users.Include(u => u.enrolledChats).FirstOrDefaultAsync(u => u.ID == UserID))!;
        Dictionary<string, long> chatsAvblToUsr = new Dictionary<string, long>();
        foreach (Chat i in user.enrolledChats)
        {
            chatsAvblToUsr.Add($"{i.Name}", (long)i.ID!);
        }
        return chatsAvblToUsr;
    }

    public async Task SendMsgToChatAsync(long userid, SendChatMsgDTO scmDTO)
    {
        User user = (await _db.Users.FirstOrDefaultAsync(u => u.ID == userid))!;
        Chat? chatToSendTo = await _db
                                    .Chats
                                    .Include(cht => cht.Members)
                                    .FirstOrDefaultAsync(cht => cht.ID == scmDTO.ChatID);
        if (chatToSendTo == null) throw new ChatDoesntExistException();
        if (!chatToSendTo.Members.Contains(user)) throw new NotChatMemberException();
        Message message = new Message
        {
            OriginID = userid,
            Text = scmDTO.Message,
            chat = await _db.Chats.FirstOrDefaultAsync(cht => cht.ID == scmDTO.ChatID),
            Timestamp = DateTime.UtcNow
        };


        chatToSendTo.Messages.Add(message);
        await _db.SaveChangesAsync();
    }

    public async Task<List<MessageReceivedDTO>> ReadChatMsgs(long ChatID, long UserID, int messagesSinceLast = 10)
    {
        Chat? chat = await _db.Chats.Include(cht => cht.Members).FirstOrDefaultAsync(cht => cht.ID == ChatID);
        if (chat is null) throw new ChatDoesntExistException();
        User user = (await _db.Users.FirstOrDefaultAsync(u => u.ID == UserID))!;
        if (!chat.Members.Contains(user)) throw new NotChatMemberException();

        List<Message>? messages = await _db.Messages
                                            .Include(msg => msg.OriginUser)
                                            .Where(msg => msg.ChatID == ChatID)
                                            .OrderByDescending(msg => msg.Timestamp)
                                            .Take(messagesSinceLast)
                                            .ToListAsync();

        List<MessageReceivedDTO> messagesToShow = messages
                        .ConvertAll<MessageReceivedDTO>(MessageMapper.MessageToReceivedMessage);
        return messagesToShow;
    }   
}