namespace MessagingApp.Services.Implementation;

using MessagingApp.DTO;
using MessagingApp.Exceptions;
using MessagingApp.Models;
using MessagingApp.Services;
using Microsoft.EntityFrameworkCore;
using MessagingApp.Mappers;

public class ChatService(DataContext _db, IValidationService _valSvc, ILogger<ChatService> _logger) : IChatService
{

    public async Task CreateNewChatAsync(User owner, ChatDescDTO ncDTO)
    {
        _logger.LogInformation("Creating new chat");
        await _valSvc.VerifyChatDoesntExist(ncDTO.Name);
        _logger.LogInformation("No chat with the same name, proceeding...");
        Chat chat = new Chat()
        {
            Owner = owner,
            Name = ncDTO.Name
        };
        _logger.LogInformation("Adding owner of the chat");
        chat.Members.Add(owner);
        foreach (string i in ncDTO.Members)
        {
            User? newMember = await _db.Users.FirstOrDefaultAsync(u => u.Username == i);
            if (newMember != null)
            {
                _logger.LogInformation("Appended a member to a chat");
                chat.Members.Add(newMember);
                continue;
            }
            _logger.LogWarning($"Couldn't find user \"{newMember}\"");
        }
        _logger.LogInformation("Persisting changes");
        await _db.Chats.AddAsync(chat);
        await _db.SaveChangesAsync();
    }

    public async Task<List<UserChatsDTO>> GetUserAvailableChats(long UserID)
    {
        _logger.LogInformation("Getting available chats of user");
        User user = (await _db.Users.Include(u => u.enrolledChats).FirstOrDefaultAsync(u => u.ID == UserID))!;
        List<UserChatsDTO> chatsAvblToUsr = new List<UserChatsDTO>();
        foreach (Chat i in user.enrolledChats)
        {
            chatsAvblToUsr.Add(new UserChatsDTO(){ ID = i.ID, Name=i.Name});
        }
        return chatsAvblToUsr;
    }

    public async Task<List<string>> GetChatMembersAsync(User user, long chatID)
    {
        _logger.LogInformation("Getting the chat members");
        Chat? chat = await _valSvc.VerifyChatAlreadyExists(chatID);
        if (chat.Members.Where(u => u.ID == user.ID).Count() <= 0)
        {
            _logger.LogWarning("The user is not a chat member to perform this action");
            throw new NotChatMemberException("You are not a participant of this chat");
        }
        return chat.Members.ConvertAll<string>(u => u.Username);

    }

    public async Task SendMsgToChatAsync(long userid, SendChatMsgDTO scmDTO)
    {
        _logger.LogInformation("Sending a message to a chat");
        User user = (await _db.Users.FirstOrDefaultAsync(u => u.ID == userid))!;
        Chat? chatToSendTo = await _db
                                    .Chats
                                    .Include(cht => cht.Members)
                                    .FirstOrDefaultAsync(cht => cht.ID == scmDTO.ChatID);
        if (chatToSendTo == null) {
            _logger.LogWarning("No such chat to send message to");
            throw new ChatDoesntExistException(); }
        if (!chatToSendTo.Members.Contains(user)) {
            _logger.LogWarning("This user is not a chat member to send message");
            throw new NotChatMemberException(); }
        _logger.LogInformation("Composing the message object");
        Message message = new Message
        {
            OriginID = userid,
            Text = scmDTO.Message,
            chat = await _db.Chats.FirstOrDefaultAsync(cht => cht.ID == scmDTO.ChatID),
            Timestamp = DateTime.UtcNow
        };

        _logger.LogInformation("Saving changes");
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
        _logger.LogInformation($"Reading chat messages (last:{messagesSinceLast}, skip:{skip}, polling:{polling}, unreadNotRead:{unreadNotRead})");
        Chat? chat = await _db.Chats.Include(cht => cht.Members).FirstOrDefaultAsync(cht => cht.ID == ChatID);
        List<MessageReceivedDTO> messagesToShow = new List<MessageReceivedDTO>();
        if (chat is null) {
            _logger.LogWarning("Could find the chat");
            throw new ChatDoesntExistException(); }
        User user = (await _db.Users.FirstOrDefaultAsync(u => u.ID == UserID))!;
        if (!chat.Members.Contains(user)) {
            _logger.LogWarning("This user is not this chat's member");
            throw new NotChatMemberException(); }
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

        _logger.LogInformation("Mapping messages to DTO");
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
        _logger.LogInformation("Marking the selected messages as read");
        foreach (Message m in messages)
        {
            m.isRead = true;
            m.readByUsers.Add(user);
        }

        _logger.LogInformation("Saving changes");

        await _db.SaveChangesAsync();
        return messagesToShow;
    }
    public async Task DelChatAsync(User supposedOwner, long chatID)
    {
        _logger.LogInformation("Deleting chat");
        Chat? chat = await _db.Chats.FirstOrDefaultAsync(cht => cht.ID == chatID);
        if (chat == null)
        {
            _logger.LogWarning("No such chat to delete");
            throw new ChatDoesntExistException("Such chat doesn't exist");
        }
        if (chat.OwnerID != supposedOwner.ID)
        {
            _logger.LogWarning("This user is not the owner to delete the chat");
            throw new NotChatAdminException("You are not this chat's administrator");
        }
        _logger.LogInformation("Deleting chat and persisting changes");
        _db.Chats.Remove(chat);
        await _db.SaveChangesAsync();
    }

    public async Task<int> AddMembersToChatAsync(User owner, NewMemberChatDTO nmcDTO)
    {
        _logger.LogInformation("Adding members to the chat");
        Chat? chat = await _db.Chats.Include(cht => cht.Members).FirstOrDefaultAsync(cht => cht.ID == nmcDTO.ChatID);
        if (chat is null) {
            _logger.LogWarning("Couldn't find the chat");
            throw new ChatDoesntExistException("This chat doesn't exist"); }
        List<User> users = new List<User>();
        List<string> usernamesPresent = chat.Members.ConvertAll(x=>x.Username);
        bool memberPresentOrNotAddedFlag = false;
        foreach (string username in nmcDTO.Members)
        {
            var user = await _db.Users.Include(u => u.enrolledChats).FirstOrDefaultAsync(u => u.Username == username);
            if (user != null && !usernamesPresent.Contains(username)) { users.Add(user); user.enrolledChats.Add(chat); }
            else
            {
                _logger.LogWarning("One of the users couldn't be found or they are already in the chat");
                memberPresentOrNotAddedFlag = true;
            }
        }
        _logger.LogInformation("Appending chat members and persisting changes");
        chat.Members.AddRange(users);
        await _db.SaveChangesAsync();
        if (memberPresentOrNotAddedFlag)
            throw new MemberAlreadyInChatException($"Some members may have not been added. Added {users.Count()} users");
        return users.Count();
    }
}
