namespace MessagingApp.Services.Implementation;

using MessagingApp.DTO;
using MessagingApp.Exceptions;
using MessagingApp.Models;
using MessagingApp.Services;
using Microsoft.EntityFrameworkCore;
using MessagingApp.Mappers;

public class ChatService(DataContext _db) : IChatService
{

    public async Task CreateNewChatAsync(User owner, NewChatDTO ncDTO)
    {
        if ((await _db.Chats.FirstOrDefaultAsync(cht => cht.Name == ncDTO.Name)) != null)
        {
            throw new ChatAlreadyExistsException($"Chat \"{ncDTO.Name}\" aready exists");
        }
        Console.WriteLine("In chatsvc");
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

    public async Task<List<MessageReceivedDTO>> ReadChatMsgs(   long ChatID,
                                                                long UserID,
                                                                bool? unreadNotRead = null,
                                                                int skip=0,
                                                                int messagesSinceLast = 10,
                                                                bool polling = false)
    {
        Chat? chat = await _db.Chats.Include(cht => cht.Members).FirstOrDefaultAsync(cht => cht.ID == ChatID);
        List<MessageReceivedDTO> messagesToShow = new List<MessageReceivedDTO>();
        if (chat is null) throw new ChatDoesntExistException();
        User user = (await _db.Users.FirstOrDefaultAsync(u => u.ID == UserID))!;
        if (!chat.Members.Contains(user)) throw new NotChatMemberException();
        List<Message>? messages = await _db.Messages
                                            .Include(msg => msg.OriginUser)
                                            .Include(msg => msg.readByUsers)
                                            .Where(msg => msg.ChatID == ChatID)
                                            .OrderByDescending(msg => msg.Timestamp)
                                            .Skip(skip)
                                            .Take(messagesSinceLast)
                                            .ToListAsync();

        if (polling)
        {
            messages = messages.Where(m => m.OriginID != UserID && !m.readByUsers.Contains(user)).ToList();
        }




         messagesToShow = messages.Where(x => x.isRead == false).ToList()
                .ConvertAll(MessageMapper.MessageToReceivedMessage);

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
        foreach (Message m in messages)
        {
            m.isRead = true;
            m.readByUsers.Add(user);
        }

        await _db.SaveChangesAsync();
        return messagesToShow;
    }
    public async Task DelChatAsync(User supposedOwner, long chatID)
    {
        Chat? chat = await _db.Chats.FirstOrDefaultAsync(cht => cht.ID == chatID);
        if (chat == null)
        {
            throw new ChatDoesntExistException("Such chat doesn't exist");
        }
        if (chat.OwnerID != supposedOwner.ID)
        {
            throw new NotChatAdminException("You are not this chat's administrator");
        }
        _db.Chats.Remove(chat);
        await _db.SaveChangesAsync();
    }

    public async Task<int> AddMembersToChatAsync(User owner, NewMemberChatDTO nmcDTO)
    {
        Chat? chat = await _db.Chats.Include(cht => cht.Members).FirstOrDefaultAsync(cht => cht.ID == nmcDTO.ChatID);
        if (chat is null) { throw new ChatDoesntExistException("This chat doesn't exist"); }
        List<User> users = new List<User>();
        List<string> usernamesPresent = chat.Members.ConvertAll(x=>x.Username);
        bool memberPresentOrNotAddedFlag = false;
        foreach (string username in nmcDTO.Members)
        {
            var user = await _db.Users.Include(u => u.enrolledChats).FirstOrDefaultAsync(u => u.Username == username);
            if (user != null && !usernamesPresent.Contains(username)) { users.Add(user); user.enrolledChats.Add(chat); }
            else { memberPresentOrNotAddedFlag = true; }
        }



        chat.Members.AddRange(users);
        await _db.SaveChangesAsync();
        if (memberPresentOrNotAddedFlag)
            throw new MemberAlreadyInChatException($"Some members may have not been added. Added {users.Count()} users");
        return users.Count();
    }
}
