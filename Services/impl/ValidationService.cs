using MessagingApp.DTO;
using MessagingApp.Exceptions;
using MessagingApp.Models;
using MessagingApp.Services;
using Microsoft.EntityFrameworkCore;

namespace MessagingApp.Validator;


public class ValidationService(DataContext _db) : IValidationService
{
    public async Task AddUserValidateAsync(AuthDTO authDTO)
    {
        if (authDTO.Username == null
            || authDTO.Password == null
            || String.Equals(authDTO.Password, "")
            || String.Equals(authDTO.Username, "")) throw new InvalidFormException();
        if (authDTO.Username.Count() < 6) throw new UsernameTooShortException();
        if (authDTO.Password.Count() < 8) throw new PasswordTooShortException();
    }

    public async Task<Chat> VerifyChatDoesntExist(object chatIdentifier)
    {
        Chat? chat;
        if (chatIdentifier is long chatId)
        {
            chat = await _db.Chats.FindAsync(chatId);
            if (chat != null)
                throw new ChatAlreadyExistsException("This chat already exists");
        }
        else if (chatIdentifier is string chatName)
        {
            chat = await _db.Chats.FirstOrDefaultAsync(c => c.Name == chatName);
            if (chat != null)
                throw new ChatAlreadyExistsException("This chat already exists");
        }
        else
        {
            throw new ArgumentException("Chat identifier must be an int or string.");
        }
        return chat;
    }

    public async Task<Chat> VerifyChatAlreadyExists(object chatIdentifier)
    {
        Chat? chat;
        if (chatIdentifier is long chatId)
        {
            chat = await _db.Chats.Include(cht => cht.Members).FirstOrDefaultAsync(c => c.ID == chatId);
            if (chat == null)
                throw new ChatDoesntExistException("This chat doesn't exist");
        }
        else if (chatIdentifier is string chatName)
        {
            chat = await _db.Chats.Include(cht => cht.Members).FirstOrDefaultAsync(c => c.Name == chatName);
            if (chat == null)
                throw new ChatDoesntExistException("This chat doesn't exist");
        }
        else
        {
            throw new ArgumentException("Chat identifier must be an int or string.");
        }
        return chat;
    }

        public async Task<User> VerifyUserAlreadyExists(string username)
    {
        User? user;
            user = await _db.Users.FirstOrDefaultAsync(c => c.Username == username);
            if (user == null)
                throw new UserDoesntExistException("This user doesn't exist");
        return user;
    }

}